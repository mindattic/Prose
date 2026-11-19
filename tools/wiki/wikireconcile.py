"""Prose <-> MediaWiki reconciliation: the two systems of record check each other (PRS-T1048).

Prose (SQL Server, through the Hub) is the canon: typed records, the books' beats, the write gate. MediaWiki is the
universe as people read and edit it: a live link graph, categories, red links, orphans, page history. Each catches what
the other cannot see:

  Prose checks the wiki   every page the sync built is on the wiki with the same text (revision sha1s, no download);
                          pages missing, pages left behind, pages changed on the wiki by a person.
  The wiki checks Prose   MediaWiki's own maintenance reports, run on the live site: wanted pages (red links: names
                          used with no entry), lonely pages (entries nothing links to), dead-end pages (entries that
                          link to nothing); and, from the books, who shares scenes with whom versus the relationships
                          Prose records (missing, one-sided, never together on the page).
  People correct Prose    a person's edit to an entry on the wiki becomes field-level proposals against the Prose
                          record; the page is held (the sync does not overwrite it) until each proposal is accepted
                          (written to Prose through the Hub, then the sync carries it back) or rejected.

Commands (state in %LOCALAPPDATA%\\MindAttic\\Prose\\wiki\\<universe>\\):
  check   build the report: out\\reconcile.json + reconcile.md, out\\held.json, and the wiki page
          Project:Reconciliation. Exit 0. Needs the last sync's out\\pages.json, beats\\, the export and interchange.
  poll    exit 10 when someone other than the sync edited the wiki since the last check, else 0 (one API call).
  decide  <id ...> --accept | --reject [--type KIND]   record a decision on proposals.
  apply   write accepted proposals to Prose through the Hub (--set-entity-fields), then mark them applied.
"""
from __future__ import annotations

import argparse
import collections
import datetime as dt
import hashlib
import html
import json
import os
import re
import sys
import tempfile
import unicodedata
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import prose2wiki as p2w  # noqa: E402

BOT_PREFIX = "prose2wiki"
REPORT_PAGE = "Project:Reconciliation"
SKIP_SECTIONS = {"Appears in", "Related", "Tags"}
MARKER = re.compile(r"<!-- prose guid ([0-9a-f-]{36}) -->")


def state_dir(universe: str) -> Path:
    return Path(os.environ["LOCALAPPDATA"]) / "MindAttic" / "Prose" / "wiki" / universe.lower()


def load_json(path: Path, default):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))  # watch.json may carry PowerShell's BOM
    except (OSError, ValueError):
        return default


def save_json(path: Path, value):
    path.write_text(json.dumps(value, indent=1, ensure_ascii=False), encoding="utf-8")


def wiki_sha1(text: str) -> str:
    """MediaWiki's stored sha1 of a page as we sent it (it normalizes to NFC, trims trailing whitespace, uses \\n)."""
    text = unicodedata.normalize("NFC", text.replace("\r\n", "\n")).rstrip()
    return hashlib.sha1(text.encode("utf-8")).hexdigest()


# ---- wikitext -> plain values ----------------------------------------------------------------------------------------
def plain(s: str) -> str:
    s = re.sub(r"<!--.*?-->", "", s, flags=re.S)
    s = re.sub(r"\[\[(?:[^\]|]*\|)?([^\]]*)\]\]", r"\1", s)
    s = re.sub(r"'{2,}", "", s)
    s = re.sub(r"</?(?:nowiki|i|b|span|div)[^>]*>", "", s)
    s = html.unescape(html.unescape(s))
    s = s.replace("—", "-").replace("&mdash;", "-")
    return re.sub(r"[ \t]+", " ", s).strip()


def parse_entity(text: str) -> dict[str, str]:
    """Label -> plain value for an entity page's infobox rows, aliases and sections."""
    fields: dict[str, str] = {}
    section, body = None, []

    def close():
        if section and section not in SKIP_SECTIONS:
            fields[section] = "\n".join(plain(l) for l in body if l.strip()).strip()

    for line in text.split("\n"):
        if line.startswith("{{Infobox row|1="):
            label, _, value = line[len("{{Infobox row|1="):].partition("|2=")
            fields[plain(label)] = plain(value[:-2] if value.endswith("}}") else value)
        elif line.startswith("| aliases ="):
            fields["Aliases"] = plain(line.split("=", 1)[1])
        elif re.match(r"^==[^=].*==\s*$", line):
            close()
            section, body = line.strip("= ").strip(), []
        elif line.startswith(("[[Category:", "<!-- prose guid")):
            close()
            section, body = None, []
        elif section:
            body.append(line)
    close()
    return fields


def paragraphs(text: str) -> list[str]:
    """A chapter page's prose, one plain paragraph each (navigation and markup lines dropped)."""
    out = []
    for block in re.split(r"\n\s*\n", text):
        b = block.strip()
        if not b or b.startswith(("{{", "__", "<div", "</div", "[[Category")) or "prose-chapnav" in b:
            continue
        out.append(plain(b))
    return [p for p in out if p]


# ---- the wiki --------------------------------------------------------------------------------------------------------
def human(rev: dict) -> bool:
    return not (rev.get("comment") or "").startswith(BOT_PREFIX)


def live_revisions(wiki: p2w.Wiki, titles: list[str]) -> dict[str, dict]:
    """Title -> latest revision {revid, sha1, user, comment, timestamp}, or {} when the page is missing."""
    found: dict[str, dict] = {}
    for i in range(0, len(titles), 50):
        batch = titles[i:i + 50]
        r = wiki.post(action="query", prop="revisions", rvprop="ids|sha1|user|comment|timestamp", titles="|".join(batch))
        q = r.get("query", {})
        names = {n["from"]: n["to"] for n in q.get("normalized", [])}
        pages = {p["title"]: p for p in q.get("pages", [])}
        for t in batch:
            p = pages.get(names.get(t, t)) or {}
            rev = (p.get("revisions") or [{}])[0]
            found[t] = {} if p.get("missing") or not rev else rev
        if (i // 50) % 40 == 0:
            print(f"  read {min(i + 50, len(titles))}/{len(titles)} live revisions", flush=True)
    return found


def all_titles(wiki: p2w.Wiki, namespaces=(0, 14, 3000)) -> dict[str, dict]:
    """Every page in the content namespaces with its latest revision's user and comment."""
    out: dict[str, dict] = {}
    for ns in namespaces:
        cont = {}
        while True:
            r = wiki.get(action="query", generator="allpages", gapnamespace=ns, gaplimit="max",
                         prop="revisions", rvprop="ids|user|comment", **cont)
            for p in r.get("query", {}).get("pages", []):
                out[p["title"]] = (p.get("revisions") or [{}])[0]
            if "continue" not in r:
                break
            cont = r["continue"]
    return out


def querypage(wiki: p2w.Wiki, name: str, limit: int = 50000) -> list[dict]:
    rows, cont = [], {}
    while len(rows) < limit:
        r = wiki.get(action="query", list="querypage", qppage=name, qplimit="max", **cont)
        qp = r.get("query", {}).get("querypage", {})
        rows += qp.get("results", [])
        if "continue" not in r:
            break
        cont = r["continue"]
    return rows


# ---- the report ------------------------------------------------------------------------------------------------------
class Report:
    def __init__(self):
        self.sections: dict[str, list[dict]] = collections.OrderedDict()
        self.proposals: list[dict] = []

    def add(self, section: str, **row):
        self.sections.setdefault(section, []).append(row)

    def propose(self, kind: str, **row) -> dict:
        key = json.dumps([kind, row.get("guid") or row.get("title"), row.get("field") or row.get("pair"),
                          row.get("wiki") or row.get("target"), row.get("revid")], ensure_ascii=False)
        row = {"id": hashlib.sha1(key.encode()).hexdigest()[:10], "kind": kind, **row}
        self.proposals.append(row)
        return row


def check(args):
    st = state_dir(args.universe)
    out = st / "out"
    pages: dict[str, str] = load_json(out / "pages.json", {})
    if not pages:
        sys.exit(f"no {out / 'pages.json'}: run the sync first")
    decisions = load_json(st / "decisions.json", {})
    rep = Report()
    wiki = p2w.Wiki(Path(args.cred))

    # ---- Prose checks the wiki: every built page is there, unchanged ------------------------------------------------
    ours = [t for t in pages if not t.startswith("MediaWiki:")]
    print(f"checking {len(ours)} built pages against the live wiki", flush=True)
    live = live_revisions(wiki, ours)
    edited: dict[str, dict] = {}
    for t in ours:
        rev = live.get(t) or {}
        if not rev:
            rep.add("Built but missing on the wiki (the next sync pushes it)", title=t)
        elif rev.get("sha1") != wiki_sha1(pages[t]):
            if human(rev):
                edited[t] = rev
            else:
                rep.add("Older than Prose (the next sync pushes it)", title=t)
    everything = all_titles(wiki)
    built = set(pages)
    leftover: list[str] = []
    for t, rev in sorted(everything.items()):
        if t in built or t == "Main Page":
            continue
        if human(rev):
            rep.add("Pages people made on the wiki (not from Prose)", title=t, user=rev.get("user"), comment=rev.get("comment"))
            rep.propose("new-page", title=t, user=rev.get("user"), revid=rev.get("revid"))
        else:
            rep.add("Left behind by the sync (no Prose entity builds it any more)", title=t)
            leftover.append(t)
    if args.tidy and leftover:
        # Only pages whose last edit was the sync's own: nobody's work is lost, and Prose no longer builds them.
        wiki.delete(leftover, f"{BOT_PREFIX}: left behind; no Prose entity builds this page any more")

    # ---- people's edits become proposals against Prose ------------------------------------------------------------
    held: dict[str, int] = {}
    release: list[str] = []
    if edited:
        texts = wiki.current(list(edited))
        beats = {p.stem: json.loads(p.read_text(encoding="utf-8")) for p in (st / "beats").glob("*.json")}
        for t, rev in edited.items():
            now = texts.get(t, "")
            m = MARKER.search(pages[t])
            if m:
                before, after = parse_entity(pages[t]), parse_entity(now)
                for label in sorted(set(before) | set(after)):
                    if before.get(label, "") != after.get(label, ""):
                        rep.propose("field", title=t, guid=m.group(1), field=label, prose=before.get(label, ""),
                                    wiki=after.get(label, ""), user=rev.get("user"), revid=rev.get("revid"))
            elif t.startswith("Book:") and "/" in t:
                old, new = paragraphs(pages[t]), paragraphs(now)
                gone = [p for p in old if p not in set(new)]
                came = [p for p in new if p not in set(old)]
                for i, para in enumerate(gone):
                    rep.propose("book-text", title=t, prose=para, wiki=came[i] if i < len(came) else "",
                                beat=find_beat(beats, para), user=rev.get("user"), revid=rev.get("revid"))
                for para in came[len(gone):]:
                    rep.propose("book-text", title=t, prose="", wiki=para, beat=None, user=rev.get("user"), revid=rev.get("revid"))
            else:
                rep.propose("page", title=t, user=rev.get("user"), revid=rev.get("revid"))
            open_ = [p for p in rep.proposals if p["title"] == t and p["id"] not in decisions]
            if open_:
                held[t] = rev.get("revid")
            else:
                release.append(t)
            rep.add("Changed on the wiki by a person", title=t, user=rev.get("user"), comment=rev.get("comment"),
                    proposals=len([p for p in rep.proposals if p["title"] == t]), held=t in held)

    # ---- the wiki checks Prose: MediaWiki's maintenance reports on the live site ------------------------------------
    redlinks = [r for r in querypage(wiki, "Wantedpages") if r.get("title") not in set(leftover)]
    for row in redlinks:
        rep.add("Wanted pages: linked, but no entry exists", title=row.get("title"), links=row.get("value"))
    lonely = [r["title"] for r in querypage(wiki, "Lonelypages") if r.get("ns") == 0]
    deadend = [r["title"] for r in querypage(wiki, "Deadendpages") if r.get("ns") == 0]
    entity_titles = {t for t, text in pages.items() if MARKER.search(text)}
    for t in lonely:
        if t in entity_titles:
            rep.add("Lonely entries: nothing links here (no relationship, no book mention)", title=t)
    for t in deadend:
        if t in entity_titles:
            rep.add("Dead-end entries: link to nothing (a thin record)", title=t)

    # ---- the wiki as the media store: pictures people uploaded for entries (PRS-T1053) ------------------------------
    cont = {}
    while True:
        r = wiki.get(action="query", list="allimages", aiprop="user|comment|url", ailimit="max", **cont)
        for img in r.get("query", {}).get("allimages", []):
            stem, _, ext = img["name"].replace("_", " ").rpartition(".")
            if ext.lower() in ("png", "jpg", "webp") and stem in entity_titles and human(img):
                rep.add("Pictures people added to entries (the wiki stores them; Prose can link them)", title=stem,
                        file=img["name"], user=img.get("user"), url=img.get("url"))
        if "continue" not in r:
            break
        cont = r["continue"]

    # ---- the books check the relationships ------------------------------------------------------------------------
    graph(args, st, rep, decisions)

    # ---- write ---------------------------------------------------------------------------------------------------
    for p in rep.proposals:
        p["decision"] = decisions.get(p["id"], {}).get("decision")
    save_json(out / "reconcile.json", {"at": dt.datetime.now(dt.timezone.utc).isoformat(), "sections": rep.sections,
                                       "proposals": rep.proposals})
    save_json(out / "held.json", held)
    if release:
        # Every proposal on these pages is decided (accepted ones are in Prose now): the next sync writes Prose's page
        # over the person's version, so the sync forgets it last pushed them.
        pushed = load_json(out / "pushed.json", {})
        for t in release:
            pushed.pop(t, None)
        save_json(out / "pushed.json", pushed)
        print(f"released {len(release)} decided page(s) for the next sync: {', '.join(release[:10])}")
    md = report_markdown(rep)
    (out / "reconcile.md").write_text(md, encoding="utf-8")
    if not args.no_publish:
        r = wiki.post(action="edit", title=REPORT_PAGE, text=report_wikitext(args.universe, rep),
                      summary=f"{BOT_PREFIX}: reconciliation report", bot="1", token=wiki.csrf)
        print("published", REPORT_PAGE, r.get("edit", {}).get("result"))
    watch = load_json(st / "watch.json", {})
    watch["wikiCheckedAt"] = dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    save_json(st / "watch.json", watch)
    counts = ", ".join(f"{k.split(':')[0]}={len(v)}" for k, v in rep.sections.items())
    print(f"proposals {len(rep.proposals)} ({sum(1 for p in rep.proposals if not p['decision'])} open), held pages {len(held)}; {counts}")


def find_beat(beats: dict[str, list], para: str):
    probe = para[:80]
    for book, rows in beats.items():
        for b in rows:
            if probe and probe in plain(p2w.TAG_RE.sub(r"\3", b.get("text") or "")):
                return {"book": book, "beat": b.get("number")}
    return None


def graph(args, st: Path, rep: Report, decisions: dict):
    """Who shares beats in the books versus the relationships Prose records."""
    zip_path = Path((st / "last-export.txt").read_text(encoding="utf-8").strip())
    repair = p2w.Repair()
    entities = p2w.load_entities(zip_path, st / f"{args.universe.lower()}.universe.json", repair)
    p2w.assign_titles(entities, repair)
    resolver = p2w.Resolver(entities)
    renderer = p2w.Renderer(entities, resolver, repair, args.universe)
    related: dict[frozenset, set] = collections.defaultdict(set)
    for e in entities.values():
        for rel in e.record.get("_relations") or []:
            t = renderer.by_slug.get(p2w.slug_key(rel.get("to") or ""))
            if t and t is not e:
                related[frozenset((e.guid, t.guid))].add(f"{e.guid}>{rel.get('kind')}")
        for rel in e.record.get("relationships") or []:
            name = rel.get("name") if isinstance(rel, dict) else rel if isinstance(rel, str) else None
            t = resolver.by_name(name) if isinstance(name, str) else None
            if t and t is not e:
                related[frozenset((e.guid, t.guid))].add(f"{e.guid}>{(rel.get('type') if isinstance(rel, dict) else '') or 'related'}")
    shared: collections.Counter = collections.Counter()
    books_of: dict[frozenset, set] = collections.defaultdict(set)
    present: set[str] = set()
    for path in (st / "beats").glob("*.json"):
        for beat in json.loads(path.read_text(encoding="utf-8")):
            ids = sorted({p2w.norm(g) for repo, g, _ in p2w.TAG_RE.findall(beat.get("text") or "")
                          if repo != "none" and p2w.norm(g) in entities})
            present.update(ids)
            for i in range(len(ids)):
                for j in range(i + 1, len(ids)):
                    pair = frozenset((ids[i], ids[j]))
                    shared[pair] += 1
                    books_of[pair].add(path.stem)
    rejected = {d.get("pair") for d in decisions.values() if d.get("decision") == "reject" and d.get("pair")}
    n = 0
    for pair, count in shared.most_common():
        if count < args.min_shared or n >= args.max_suggestions:
            break
        if pair in related:
            continue
        a, b = (entities[g] for g in sorted(pair))
        key = f"{a.guid}|{b.guid}"
        if key in rejected:
            continue
        n += 1
        rep.propose("relationship", title=a.title, guid=a.guid, target=b.title, target_guid=b.guid, pair=key,
                    shared_beats=count, books=sorted(books_of[pair]))
        rep.add("Share scenes but Prose records no relationship", a=a.title, b=b.title, beats=count,
                books=len(books_of[pair]))
    for pair, kinds in related.items():
        ids = sorted(pair)
        if all(g in present for g in ids) and shared[pair] == 0:
            a, b = (entities[g] for g in ids)
            rep.add("Related in Prose, never in the same beat of any book (backstory, or wrong)", a=a.title, b=b.title,
                    kinds=sorted(k.split(">", 1)[1] for k in kinds))
    for pair, kinds in related.items():
        sides = {k.split(">", 1)[0] for k in kinds}
        ids = sorted(pair)
        if len(sides) == 1 and all(g in present for g in ids):
            one = next(iter(sides))
            other = ids[0] if ids[1] == one else ids[1]
            rep.add("One-sided: only one of the two records names the other", has=entities[one].title,
                    missing=entities[other].title)


def report_markdown(rep: Report) -> str:
    lines = ["# Prose <-> wiki reconciliation", ""]
    open_ = [p for p in rep.proposals if not p.get("decision")]
    lines += [f"## Open proposals ({len(open_)})", ""]
    lines += [f"- `{p['id']}` {p['kind']}: {json.dumps({k: v for k, v in p.items() if k not in ('id', 'kind', 'decision')}, ensure_ascii=False)[:400]}"
              for p in open_[:500]]
    for name, rows in rep.sections.items():
        lines += ["", f"## {name} ({len(rows)})", ""]
        lines += [f"- {json.dumps(r, ensure_ascii=False)}" for r in rows[:300]]
        if len(rows) > 300:
            lines.append(f"- ... {len(rows) - 300} more in reconcile.json")
    return "\n".join(lines) + "\n"


def report_wikitext(universe: str, rep: Report) -> str:
    def link(t):
        return f"[[:{t}]]" if t else ""
    lines = ["__NOEDITSECTION__",
             f"How the {universe} wiki and the Prose database check each other, updated by every reconciliation run. "
             "Prose holds the canon; this wiki shows it and lets people correct it. Edits people make to an entry are "
             "held here as proposals until they are accepted into Prose or turned down.", ""]
    open_ = [p for p in rep.proposals if not p.get("decision")]
    lines += [f"== Proposals waiting for a decision ({len(open_)}) ==",
              '{| class="wikitable sortable"', "! Id !! Kind !! Page !! What"]
    for p in open_[:400]:
        if p["kind"] == "field":
            what = f"''{p['field']}'': {p_esc(p['prose'])[:160]} &rarr; {p_esc(p['wiki'])[:160]} (by {p.get('user')})"
        elif p["kind"] == "relationship":
            what = f"with {link(p['target'])}: together in {p['shared_beats']} beats"
        elif p["kind"] == "book-text":
            what = f"{p_esc(p['prose'])[:120]} &rarr; {p_esc(p['wiki'])[:120]}"
        else:
            what = f"by {p.get('user')}"
        lines += ["|-", f"| <code>{p['id']}</code> || {p['kind']} || {link(p.get('title'))} || {what}"]
    lines += ["|}", ""]
    for name, rows in rep.sections.items():
        lines += [f"== {name} ({len(rows)}) =="]
        # Pages that don't exist are named, not linked: a link would make the report itself a red link source.
        gone = name.startswith(("Left behind", "Wanted pages", "Built but missing"))
        for r in rows[:200]:
            bits = [(p_esc(str(v)) if gone else link(v)) if k in ("title", "a", "b", "has", "missing") else p_esc(str(v))
                    for k, v in r.items() if v not in (None, "", [], False)]
            lines.append("* " + " &middot; ".join(bits))
        if len(rows) > 200:
            lines.append(f"* &hellip; {len(rows) - 200} more")
        lines.append("")
    lines.append("[[Category:Maintenance]]")
    return "\n".join(lines) + "\n"


def p_esc(s: str) -> str:
    return p2w.escape(str(s).replace("\n", " "))


# ---- poll, decide, apply ---------------------------------------------------------------------------------------------
def poll(args) -> int:
    st = state_dir(args.universe)
    watch = load_json(st / "watch.json", {})
    since = watch.get("wikiCheckedAt")
    wiki = p2w.Wiki(Path(args.cred))
    params = dict(action="query", list="recentchanges", rcprop="title|user|comment|timestamp", rctype="edit|new",
                  rclimit="max", rcdir="newer")
    if since:
        params["rcstart"] = since
    rows = wiki.get(**params).get("query", {}).get("recentchanges", [])
    people = [r for r in rows if human(r) and r.get("title") != REPORT_PAGE]
    for r in people[:20]:
        print(f"  {r['timestamp']} {r['user']}: {r['title']} ({r.get('comment') or 'no summary'})")
    print(f"{len(people)} edit(s) by people since {since or 'ever'}")
    return 10 if people else 0


def decide(args):
    st = state_dir(args.universe)
    decisions = load_json(st / "decisions.json", {})
    proposals = {p["id"]: p for p in load_json(st / "out" / "reconcile.json", {}).get("proposals", [])}
    for pid in args.ids:
        p = proposals.get(pid)
        if not p:
            print(f"  no proposal {pid} in the last report")
            continue
        decisions[pid] = {"decision": "accept" if args.accept else "reject", "kind": p["kind"], "pair": p.get("pair"),
                          "type": args.type, "at": dt.datetime.now(dt.timezone.utc).isoformat()}
        print(f"  {pid} {decisions[pid]['decision']}ed ({p['kind']} {p.get('title')})")
    save_json(st / "decisions.json", decisions)


def hub(handler: str, *args: str, universe: str) -> tuple[int, str]:
    import requests
    settings = json.loads((Path(os.environ["LOCALAPPDATA"]) / "MindAttic" / "Prose" / "Settings.json").read_text(encoding="utf-8-sig"))
    headers = {"X-Prose-Key": settings["HubApiKey"]} if settings.get("HubApiKey") else {}
    r = requests.post("http://127.0.0.1:5900/api/cli-invoke", headers=headers, timeout=(10, 600), json={
        "handlerClass": handler, "args": ["--universe", universe, *args], "universe": universe,
        "cwd": str(Path(__file__).resolve().parents[2])})
    r.raise_for_status()
    j = r.json()
    return j["exitCode"], (j.get("output") or "") + (j.get("error") or "")


def apply(args):
    """Accepted field and relationship proposals become Prose writes; book text and new pages stay for a person."""
    st = state_dir(args.universe)
    decisions = load_json(st / "decisions.json", {})
    report = load_json(st / "out" / "reconcile.json", {})
    zip_path = Path((st / "last-export.txt").read_text(encoding="utf-8").strip())
    entities = p2w.load_entities(zip_path, None, p2w.Repair())
    writes: dict[str, dict] = collections.defaultdict(dict)
    done: list[str] = []
    for p in report.get("proposals", []):
        d = decisions.get(p["id"]) or {}
        if d.get("decision") != "accept" or d.get("applied"):
            continue
        ent = entities.get(p2w.norm(p.get("guid") or ""))
        if ent is None:
            print(f"  {p['id']}: no entity {p.get('guid')}")
            continue
        rec = ent.record
        if p["kind"] == "field":
            key = next((k for k in rec if p2w.humanize(k) == p["field"]), None)
            if p["field"] == "Aliases":
                key = "aliases" if "aliases" in rec or "common_names" not in rec else "common_names"
            if key is None and p["field"] == "Description":
                key = "summary"
            if key is None:
                print(f"  {p['id']}: no field '{p['field']}' on {ent.title}; left for a person")
                continue
            old = rec.get(key)
            value = p["wiki"]
            if isinstance(old, list) and all(isinstance(v, str) for v in old):
                value = [v.strip() for v in re.split(r",\s*|\n", value) if v.strip()]
            elif isinstance(old, bool):
                value = value.lower() in ("yes", "true")
            elif isinstance(old, int):
                value = int(value) if value.lstrip("-").isdigit() else value
            elif not isinstance(old, (str, type(None))):
                print(f"  {p['id']}: '{key}' on {ent.title} is structured; left for a person")
                continue
            writes[ent.guid][key] = value or None
            done.append(p["id"])
        elif p["kind"] == "relationship":
            rels = rec.get("relationships")
            if not isinstance(rels, list):
                print(f"  {p['id']}: {ent.title} keeps no relationships list; left for a person")
                continue
            current = writes[ent.guid].get("relationships") or list(rels)
            current.append({"name": p["target"], "type": d.get("type") or "associate",
                            "description": f"Together in {p['shared_beats']} beats of the books."})
            writes[ent.guid]["relationships"] = current
            done.append(p["id"])
    for guid, fields in writes.items():
        with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False, encoding="utf-8") as f:
            json.dump(fields, f, ensure_ascii=False)
        code, out = hub("WorldCli", "--set-entity-fields", "--id", p2w.dashed(guid), "--file", f.name,
                        "--confirm-unread", universe=args.universe)
        os.unlink(f.name)
        print(f"  {p2w.dashed(guid)} {sorted(fields)}: {'ok' if code == 0 else f'FAILED ({code}) {out.strip()[:300]}'}")
        if code != 0:
            done = [i for i in done if (next((p for p in report['proposals'] if p['id'] == i), {}).get('guid') or '').replace('-', '') != guid]
    for pid in done:
        decisions[pid]["applied"] = dt.datetime.now(dt.timezone.utc).isoformat()
    save_json(st / "decisions.json", decisions)
    print(f"applied {len(done)} proposal(s); the next sync carries them to the wiki")


def main():
    p = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    p.add_argument("command", choices=["check", "poll", "decide", "apply"])
    p.add_argument("ids", nargs="*")
    p.add_argument("--universe", default="glmz")
    p.add_argument("--cred")
    p.add_argument("--accept", action="store_true")
    p.add_argument("--reject", action="store_true")
    p.add_argument("--type", help="decide --accept on a relationship: its kind (default associate)")
    p.add_argument("--min-shared", type=int, default=6, help="beats two entries must share to suggest a relationship")
    p.add_argument("--max-suggestions", type=int, default=150)
    p.add_argument("--no-publish", action="store_true", help="check: don't write the wiki's report page")
    p.add_argument("--tidy", action="store_true", help="check: delete pages the sync made that Prose no longer builds")
    args = p.parse_args()
    args.cred = args.cred or os.path.expandvars(rf"%APPDATA%\MindAttic\Wiki\{args.universe.lower()}.json")
    if args.command == "check":
        check(args)
    elif args.command == "poll":
        sys.exit(poll(args))
    elif args.command == "decide":
        if args.accept == args.reject:
            sys.exit("decide needs --accept or --reject")
        decide(args)
    else:
        apply(args)


if __name__ == "__main__":
    main()
