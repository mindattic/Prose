"""Prose universe -> MediaWiki: builds one article per entity from a `prose --export global` zip, links entities to
each other, writes a validation/repair report, and pushes the pages through the MediaWiki API.

Inputs (all produced by the Prose CLI, never by reading the database directly):
  --export-zip   prose --universe <u> --export global
  --interchange  prose --universe-export <u> <path>      (adds entity types the zip lacks, e.g. bestiary)
  --beats-dir    prose --universe <u> --read-beats --slug <book> --format json, one <book>.json per canon book
Outputs in --out: pages/<n>.wiki + pages.json (title -> text), repair.json, repair.md, presence.json.
"""
from __future__ import annotations

import argparse
import collections
import hashlib
import html
import json
import os
import re
import sys
import time
import zipfile
from dataclasses import dataclass, field
from pathlib import Path

ZERO_GUID = "00000000000000000000000000000000"
GUID_RE = re.compile(r"\b[0-9a-fA-F]{8}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{12}\b")
TAG_RE = re.compile(r'<entity repo="([^"]*)" guid="([0-9a-fA-F-]{32,36})">(.*?)</entity>', re.S)
TITLE_FIELDS = ("name", "title", "term", "headline", "codename")
HIDDEN_FIELDS = {"id", "rating", "vote_count", "image_prompt", "dalle3_prompt", "changelog", "provenance",
                 "file_name", "line_count", "headings", "number", "story_id"}
SHORT = 140
INVALID_TITLE = re.compile(r"[#<>\[\]|{}_\x00-\x1f]")


def norm(guid: str) -> str:
    return guid.replace("-", "").lower()


def dashed(guid: str) -> str:
    g = norm(guid)
    return f"{g[:8]}-{g[8:12]}-{g[12:16]}-{g[16:20]}-{g[20:]}"


def slug_key(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", "_", text.lower()).strip("_")


def humanize(key: str) -> str:
    return key.replace("_", " ").strip().capitalize()


def repo_label(repo: str) -> str:
    return repo.replace("_", " ").strip().title()


@dataclass
class Entity:
    guid: str
    repo: str
    name: str
    record: dict
    aliases: list[str] = field(default_factory=list)
    title: str = ""
    books: dict[str, int] = field(default_factory=dict)


class Repair:
    def __init__(self):
        self.items: dict[str, list] = collections.defaultdict(list)

    def add(self, problem: str, **detail):
        self.items[problem].append(detail)

    def write(self, out: Path):
        (out / "repair.json").write_text(json.dumps(self.items, indent=1, ensure_ascii=False), encoding="utf-8")
        lines = ["# Prose -> wiki validation report", ""]
        for kind, rows in sorted(self.items.items()):
            lines += [f"## {kind} ({len(rows)})", ""]
            lines += [f"- {json.dumps(r, ensure_ascii=False)}" for r in rows[:500]]
            if len(rows) > 500:
                lines.append(f"- ... {len(rows) - 500} more in repair.json")
            lines.append("")
        (out / "repair.md").write_text("\n".join(lines), encoding="utf-8")


def entity_name(record: dict) -> str:
    for key in TITLE_FIELDS:
        value = record.get(key)
        if isinstance(value, str) and value.strip():
            return value.strip()
    quote = record.get("quote")
    if isinstance(quote, str) and quote.strip():
        words = quote.strip().split()
        return " ".join(words[:8]) + (" ..." if len(words) > 8 else "")
    file_name = record.get("file_name")
    if isinstance(file_name, str) and file_name.strip():
        return re.sub(r"[_-]+", " ", Path(file_name).stem).strip().capitalize()
    return ""


def load_entities(export_zip: Path, interchange: Path | None, repair: Repair) -> dict[str, Entity]:
    entities: dict[str, Entity] = {}
    with zipfile.ZipFile(export_zip) as z:
        for entry in z.namelist():
            if not entry.endswith(".json"):
                continue
            record = json.loads(z.read(entry))
            repo = entry.split("/")[0]
            guid = norm(str(record.get("id", "")))
            name = entity_name(record)
            if not any(isinstance(record.get(k), str) and record[k].strip() for k in (*TITLE_FIELDS, "quote")):
                repair.add("record without a title", repo=repo, guid=dashed(guid), file=entry, used=name or None)
            if not name:
                name = f"{repo_label(repo)} {guid[:8]}"
            aliases = [a for a in (record.get("aliases") or record.get("common_names") or []) if isinstance(a, str)]
            entities[guid] = Entity(guid, repo, name, record, aliases)
            # The export names each file after the entity's stored slug, which is what edges point at.
            record["_slug"] = re.sub(r"-\d+$", "", Path(entry).stem)
    if interchange:
        for e in json.loads(interchange.read_text(encoding="utf-8"))["entities"]:
            guid = norm(e["guid"])
            ent = entities.get(guid)
            if ent is None:
                repo = e.get("type") or "unsorted"
                ent = entities[guid] = Entity(guid, repo, e["name"],
                                              {"description": e.get("summary") or "", "tags": e.get("tags") or []})
            ent.record["_slug"] = e["id"]
            ent.record["_relations"] = e.get("relations") or []
    for ent in entities.values():
        if "�" in json.dumps(ent.record, ensure_ascii=False):
            repair.add("text with a lost character (U+FFFD)", repo=ent.repo, name=ent.name, guid=dashed(ent.guid))
    return entities


def overlay_tagged(path: Path, entities: dict[str, Entity], repair: Repair):
    """Apply `prose --tag-entity-text --out` output: entity text with <entity> markup, so links come from canon tags."""
    for guid, v in json.loads(path.read_text(encoding="utf-8")).items():
        if guid == "_wordLikeNames":
            for word in v:
                repair.add("one-word name or alias used more as an ordinary word than as a name (tagged mid-sentence only)", text=word)
            continue
        ent = entities.get(norm(guid))
        if ent is None:
            repair.add("tagged entity missing from the export", guid=guid, name=v.get("name"))
            continue
        for key, value in (v.get("record") or {}).items():
            ent.record[key] = value
        summary = v.get("summary")
        if summary and ent.record.get("description") in (None, "", TAG_RE.sub(r"\3", summary)):
            ent.record["description"] = summary


def load_presence(beats_dir: Path | None, entities: dict[str, Entity], repair: Repair) -> list[str]:
    books: list[str] = []
    if not beats_dir:
        return books
    for path in sorted(beats_dir.glob("*.json")):
        book = path.stem
        books.append(book)
        for beat in json.loads(path.read_text(encoding="utf-8")):
            for _, guid, _ in TAG_RE.findall(beat.get("text") or ""):
                g = norm(guid)
                if g == ZERO_GUID:
                    continue
                if g not in entities:
                    repair.add("book tag pointing at no entity", book=book, beat=beat.get("number"), guid=dashed(g))
                    continue
                entities[g].books[book] = entities[g].books.get(book, 0) + 1
    return books


def assign_titles(entities: dict[str, Entity], repair: Repair):
    by_name = collections.defaultdict(list)
    for ent in entities.values():
        base = INVALID_TITLE.sub(" ", ent.name).strip(" .:/")
        base = re.sub(r"\s+", " ", base)[:200] or ent.guid[:8]
        ent.title = base[0].upper() + base[1:]
        by_name[ent.title.lower()].append(ent)
    for group in by_name.values():
        if len(group) == 1:
            continue
        repair.add("duplicate name", name=group[0].name,
                   entities=[{"repo": e.repo, "guid": dashed(e.guid)} for e in group])
        repos = collections.Counter(e.repo for e in group)
        for ent in group:
            suffix = repo_label(ent.repo) if repos[ent.repo] == 1 else f"{repo_label(ent.repo)} {ent.guid[:6]}"
            ent.title = f"{ent.title} ({suffix})"


class Resolver:
    """Exact name/alias -> entity, keeping only names that point at exactly one entity."""

    def __init__(self, entities: dict[str, Entity]):
        owners = collections.defaultdict(set)
        for ent in entities.values():
            for n in [ent.name, *ent.aliases]:
                if isinstance(n, str) and len(n.strip()) >= 3:
                    owners[n.strip().lower()].add(ent.guid)
        self.unique = {k: next(iter(v)) for k, v in owners.items() if len(v) == 1}
        self.entities = entities

    def by_name(self, text: str) -> Entity | None:
        guid = self.unique.get(text.strip().lower())
        return self.entities.get(guid) if guid else None


class Renderer:
    def __init__(self, entities: dict[str, Entity], resolver: Resolver, repair: Repair, universe: str):
        self.entities, self.resolver, self.repair, self.universe = entities, resolver, repair, universe
        self.by_slug: dict[str, Entity] = {}
        for e in entities.values():  # interchange slugs win over file-name and name-derived ones
            for key in (e.record.get("_slug"), e.name):
                if key:
                    self.by_slug.setdefault(slug_key(key), e)
        for e in entities.values():
            if e.record.get("_relations") is not None and e.record.get("_slug"):
                self.by_slug[slug_key(e.record["_slug"])] = e
        self.linked: set[str] = set()

    def link(self, ent: Entity, text: str | None = None) -> str:
        label = text if text is not None else ent.title
        return f"[[{ent.title}]]" if label == ent.title else f"[[{ent.title}|{escape(label)}]]"

    def text(self, value: str, owner: Entity) -> str:
        """Long text: <entity> tags become links (first mention of each target per block); the rest is escaped."""
        out, pos, seen = [], 0, set()
        for m in TAG_RE.finditer(value):
            out.append(escape(value[pos:m.start()]))
            repo, guid, inner = m.group(1), norm(m.group(2)), m.group(3)
            target = self.entities.get(guid)
            if guid == ZERO_GUID or repo == "none" or target is None or target is owner or guid in seen:
                if target is None and guid != ZERO_GUID:
                    self.repair.add("entity tag pointing at no entity", owner=owner.title, guid=dashed(guid))
                out.append(escape(inner))
            else:
                seen.add(guid)
                self.linked.add(guid)
                out.append(self.link(target, inner))
            pos = m.end()
        out.append(escape(value[pos:]))
        return "".join(out)

    def short(self, value, owner: Entity, key: str) -> str:
        """A short scalar: a GUID or an exact entity name becomes a link."""
        s = str(value).strip()
        if GUID_RE.fullmatch(s):
            target = self.entities.get(norm(s))
            if target:
                return self.link(target)
            self.repair.add("field pointing at no entity", owner=owner.title, field=key, guid=dashed(s))
            return escape(s)
        target = self.resolver.by_name(s)
        if target and target is not owner:
            self.linked.add(target.guid)
            return self.link(target, s)
        return self.text(s, owner)

    def item(self, value, owner: Entity, key: str) -> str:
        if isinstance(value, dict):
            name = next((value[k] for k in ("name", "target", "to", "title", "term") if isinstance(value.get(k), str)), None)
            head = ""
            if name:
                head = f"'''{self.short(name, owner, key)}'''"
                if key == "relationships" and not GUID_RE.fullmatch(name) and not self.resolver.by_name(name):
                    described = (not name[:1].isupper() or re.search(r"unnamed|\(|^(?:Her|His|Their|The|A|An) [a-z]", name)
                                 is not None)
                    self.repair.add("relationship to an unnamed person (plain text, fine)" if described
                                    else "relationship names someone with no entity or alias", owner=owner.title, target=name)
            rest = [f"''{humanize(k)}:'' {self.value(v, owner, k)}" for k, v in value.items()
                    if k not in ("name", "target", "to", "title", "term") and k not in HIDDEN_FIELDS
                    and v not in (None, "", [], {})]
            return " &mdash; ".join(x for x in [head, "; ".join(rest)] if x)
        if isinstance(value, str) and len(value) <= SHORT:
            return self.short(value, owner, key)
        return self.value(value, owner, key)

    def value(self, value, owner: Entity, key: str) -> str:
        if isinstance(value, str):
            return self.short(value, owner, key) if len(value) <= SHORT and "\n" not in value else self.text(value, owner)
        if isinstance(value, bool):
            return "yes" if value else "no"
        if isinstance(value, (int, float)):
            return str(value)
        if isinstance(value, list):
            return ", ".join(self.item(v, owner, key) for v in value if v not in (None, ""))
        if isinstance(value, dict):
            return "; ".join(f"''{humanize(k)}:'' {self.value(v, owner, k)}" for k, v in value.items()
                             if k not in HIDDEN_FIELDS and v not in (None, "", [], {}))
        return escape(str(value))

    def page(self, ent: Entity, books: list[str], book_titles: dict[str, str]) -> str:
        rec = ent.record
        infobox, sections = [], []
        for key, value in rec.items():
            if key in HIDDEN_FIELDS or key.startswith("_") or key in TITLE_FIELDS or key in ("aliases", "tags"):
                continue
            if value in (None, "", [], {}):
                continue
            if isinstance(value, (str, int, float, bool)) and len(str(value)) <= SHORT and "\n" not in str(value):
                infobox.append((humanize(key), self.value(value, ent, key)))
            elif isinstance(value, list) and all(isinstance(v, str) and len(v) <= SHORT for v in value) and len(value) <= 12:
                infobox.append((humanize(key), self.value(value, ent, key)))
            elif isinstance(value, list):
                body = "\n".join(f"* {self.item(v, ent, key)}" for v in value if v not in (None, ""))
                sections.append((humanize(key), body))
            elif isinstance(value, dict):
                body = "\n".join(f"; {humanize(k)}\n: {self.value(v, ent, k)}" for k, v in value.items()
                                 if v not in (None, "", [], {}))
                sections.append((humanize(key), body))
            else:
                paras = [self.text(p.strip(), ent) for p in str(value).split("\n") if p.strip()]
                sections.append((humanize(key), "\n\n".join(paras)))
        related = [(rel.get("kind") or "related", self.by_slug.get(slug_key(rel.get("to") or ""))) for rel in rec.get("_relations") or []]
        related = [(kind, t) for kind, t in related if t is not None and t is not ent]
        if related:
            for t in (t for _, t in related):
                self.linked.add(t.guid)
            sections.append(("Related", "\n".join(f"* {self.link(t)} &mdash; {escape(humanize(kind))}"
                                                  for kind, t in related)))
        lines =["{{Infobox entity", f"| title = {escape(ent.name)}", f"| repo = {repo_label(ent.repo)}"]
        if ent.aliases:
            lines.append(f"| aliases = {escape(', '.join(ent.aliases))}")
        lines.append("}}")
        lines += [f"{{{{Infobox row|1={label}|2={val}}}}}" for label, val in infobox]
        lines.append("{{Infobox end}}")
        canon = bool(ent.books)
        intro = f"'''{escape(ent.name)}''' is an entry in the [[:Category:{repo_label(ent.repo)}|{repo_label(ent.repo)}]] " \
                f"repository of the {escape(self.universe)} universe."
        lines += ["", intro, ""]
        for heading, body in sections:
            lines += [f"== {heading} ==", body, ""]
        if canon:
            lines.append("== Appears in ==")
            for book in books:
                if book in ent.books:
                    lines.append(f"* [[:Category:Appears in {book_titles.get(book, book.upper())}|"
                                 f"{book_titles.get(book, book.upper())}]] ({ent.books[book]} mentions)")
            lines.append("")
        tags = [t for t in rec.get("tags") or [] if isinstance(t, str)]
        if tags:
            lines += ["== Tags ==", ", ".join(escape(t) for t in tags), ""]
        lines.append(f"<!-- prose guid {dashed(ent.guid)} -->")
        lines.append(f"[[Category:{repo_label(ent.repo)}]]")
        lines.append("[[Category:Canon]]" if canon else "[[Category:Background]]")
        lines += [f"[[Category:Appears in {book_titles.get(b, b.upper())}]]" for b in books if b in ent.books]
        return "\n".join(lines) + "\n"


def escape(s: str) -> str:
    s = html.unescape(s)
    s = s.replace("[[", "&#91;&#91;").replace("]]", "&#93;&#93;").replace("{{", "&#123;&#123;").replace("}}", "&#125;&#125;")
    s = s.replace("|", "&#124;").replace("~~~", "&#126;&#126;&#126;").replace("----", "&#45;---")
    s = re.sub(r"<(/?)(entity)\b[^>]*>", "", s)
    s = s.replace("<", "&lt;").replace(">", "&gt;")
    s = re.sub(r"^([*#:;=])", r"<nowiki>\1</nowiki>", s, flags=re.M)
    s = re.sub(r"'{2,}", lambda m: "&#39;" * len(m.group()), s)
    return s


TEMPLATES = {
    "Template:Infobox entity": """<includeonly><div class="prose-infobox"><div class="prose-infobox-title">{{{title}}}</div><div class="prose-infobox-repo">[[:Category:{{{repo}}}|{{{repo}}}]]</div><div class="prose-infobox-aliases">{{{aliases|}}}</div><table class="prose-infobox-rows"></includeonly><noinclude>Opens an entity infobox. Rows follow as {{tl|Infobox row}}, closed by {{tl|Infobox end}}. Generated by prose2wiki.</noinclude>""",
    "Template:Infobox row": """<includeonly><tr><th>{{{1}}}</th><td>{{{2}}}</td></tr></includeonly><noinclude>One infobox row: label, value.</noinclude>""",
    "Template:Infobox end": """<includeonly></table></div></includeonly><noinclude>Closes {{tl|Infobox entity}}.</noinclude>""",
    "Template:Tl": """<includeonly>&#123;&#123;[[Template:{{{1}}}|{{{1}}}]]&#125;&#125;</includeonly>""",
    "MediaWiki:Common.css": """/* prose2wiki infobox (the Cyberspace theme replaces these later) */
.prose-infobox { float: right; clear: right; width: 22em; margin: 0 0 1em 1em; border: 1px solid #a2a9b1; background: #f8f9fa; font-size: 88%; }
.prose-infobox-title { font-size: 125%; font-weight: bold; text-align: center; padding: .4em; }
.prose-infobox-repo, .prose-infobox-aliases { text-align: center; font-style: italic; padding: 0 .4em .3em; }
.prose-infobox-rows { width: 100%; border-collapse: collapse; }
.prose-infobox-rows th { text-align: left; vertical-align: top; padding: .2em .4em; width: 35%; }
.prose-infobox-rows td { vertical-align: top; padding: .2em .4em; }
@media (max-width: 720px) { .prose-infobox { float: none; width: auto; margin: 0 0 1em 0; } }
""",
}


def site_pages(universe: str, entities: dict[str, Entity], books: list[str], book_titles: dict[str, str]) -> dict[str, str]:
    pages = dict(TEMPLATES)
    repos = collections.Counter(e.repo for e in entities.values())
    canon = collections.Counter(e.repo for e in entities.values() if e.books)
    for repo, n in repos.items():
        pages[f"Category:{repo_label(repo)}"] = (
            f"The '''{repo_label(repo)}''' repository of the {universe} universe: {n} entries, "
            f"{canon.get(repo, 0)} of them named in the canon books.\n")
    pages["Category:Canon"] = f"Entities that appear in the prose of the {universe} canon books.\n"
    pages["Category:Background"] = f"Entities in the {universe} repositories that no canon book names yet.\n"
    for book in books:
        title = book_titles.get(book, book.upper())
        pages[f"Category:Appears in {title}"] = f"Entities named in the prose of ''{title}''.\n"
    rows = "\n".join(f"* [[:Category:{repo_label(r)}|{repo_label(r)}]] &mdash; {n} entries ({canon.get(r, 0)} in the books)"
                     for r, n in sorted(repos.items(), key=lambda x: repo_label(x[0])))
    book_rows = "\n".join(f"* [[:Category:Appears in {book_titles.get(b, b.upper())}|{book_titles.get(b, b.upper())}]]" for b in books)
    pages["Main Page"] = (f"__NOTOC__\nWelcome to the '''{universe} Wiki''', generated from the Prose canon.\n\n"
                          f"== Repositories ==\n{rows}\n\n== Books ==\n{book_rows}\n\n"
                          f"See [[:Category:Canon]] for everything the books name, and [[:Category:Background]] for the rest.\n")
    return pages


def build(args) -> dict[str, str]:
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    repair = Repair()
    entities = load_entities(Path(args.export_zip), Path(args.interchange) if args.interchange else None, repair)
    if args.tagged:
        overlay_tagged(Path(args.tagged), entities, repair)
    books = load_presence(Path(args.beats_dir) if args.beats_dir else None, entities, repair)
    book_titles = json.loads(Path(args.book_titles).read_text(encoding="utf-8")) if args.book_titles else {}
    assign_titles(entities, repair)
    resolver = Resolver(entities)
    renderer = Renderer(entities, resolver, repair, args.universe)
    for ent in entities.values():
        for rel in ent.record.get("_relations") or []:
            if slug_key(rel.get("to") or "") not in renderer.by_slug:
                repair.add("edge to a missing entity", owner=ent.title, to=rel.get("to"), kind=rel.get("kind"))
    pages = {}
    phase = args.phase
    for ent in sorted(entities.values(), key=lambda e: e.title):
        if phase == "1" and not ent.books or phase == "2" and ent.books:
            continue
        if args.repo and ent.repo not in args.repo:
            continue
        pages[ent.title] = renderer.page(ent, books, book_titles)
        for alias in ent.aliases:
            alias_title = INVALID_TITLE.sub(" ", alias).strip()
            if alias_title and resolver.by_name(alias) is ent and alias_title.lower() != ent.title.lower():
                pages.setdefault(alias_title[0].upper() + alias_title[1:], f"#REDIRECT [[{ent.title}]]\n")
    pages.update(site_pages(args.universe, entities, books, book_titles))
    if args.theme:
        theme = Path(__file__).parent / "theme"
        pages["MediaWiki:Common.css"] = (theme / f"{args.theme}.css").read_text(encoding="utf-8")
        pages["MediaWiki:Common.js"] = (theme / f"{args.theme}.js").read_text(encoding="utf-8")
    (out / "pages.json").write_text(json.dumps(pages, indent=0, ensure_ascii=False), encoding="utf-8")
    (out / "presence.json").write_text(json.dumps({dashed(e.guid): {"title": e.title, "repo": e.repo, "books": e.books}
                                                   for e in entities.values() if e.books}, indent=1, ensure_ascii=False),
                                       encoding="utf-8")
    repair.write(out)
    canon = sum(1 for e in entities.values() if e.books)
    print(f"entities {len(entities)}  canon {canon}  background {len(entities) - canon}  pages {len(pages)}  "
          f"repair kinds {', '.join(f'{k}={len(v)}' for k, v in sorted(repair.items.items()))}")
    return pages


class Wiki:
    def __init__(self, cred_path: Path):
        import requests
        self.cred = json.loads(cred_path.read_text(encoding="utf-8"))
        self.api = self.cred["api"]
        self.s = requests.Session()
        self.s.headers["User-Agent"] = "prose2wiki/1.0"
        token = self.get(action="query", meta="tokens", type="login")["query"]["tokens"]["logintoken"]
        r = self.post(action="clientlogin", username=self.cred["user"], password=self.cred["password"],
                      logintoken=token, loginreturnurl=self.cred.get("site", self.api))
        if r.get("clientlogin", {}).get("status") != "PASS":
            sys.exit(f"login failed: {r}")
        self.csrf = self.get(action="query", meta="tokens")["query"]["tokens"]["csrftoken"]

    def get(self, **params):
        return self._call("get", params)

    def post(self, **data):
        return self._call("post", data)

    def _call(self, method, params):
        params = {**params, "format": "json", "formatversion": "2"}
        for attempt in range(6):
            try:
                r = (self.s.get(self.api, params=params, timeout=60) if method == "get"
                     else self.s.post(self.api, data=params, timeout=120))
                if r.status_code in (429, 500, 502, 503, 504):
                    raise RuntimeError(f"HTTP {r.status_code}")
                body = r.json()
                if body.get("error", {}).get("code") in ("maxlag", "ratelimited"):
                    raise RuntimeError(body["error"]["code"])
                return body
            except Exception as e:  # network / host throttling: back off and retry
                wait = 2 ** attempt
                print(f"  retry in {wait}s: {e}", flush=True)
                time.sleep(wait)
        sys.exit("the wiki API kept failing")

    def current(self, titles: list[str]) -> dict[str, str]:
        found = {}
        for i in range(0, len(titles), 50):
            r = self.get(action="query", prop="revisions", rvprop="content", rvslots="main", titles="|".join(titles[i:i + 50]))
            for p in r.get("query", {}).get("pages", []):
                if not p.get("missing") and p.get("revisions"):
                    found[p["title"]] = p["revisions"][0]["slots"]["main"]["content"]
        return found

    def purge(self, titles: list[str], batch: int = 20, workers: int = 1):
        """Re-render pages and refresh their link tables: pages saved before the pages they link to still show those
        links red until purged (the wiki's own job queue gets there eventually, slowly, on shared hosting)."""
        from concurrent.futures import ThreadPoolExecutor
        chunks = [titles[i:i + batch] for i in range(0, len(titles), batch)]
        with ThreadPoolExecutor(max_workers=max(1, workers)) as pool:
            for n, _ in enumerate(pool.map(
                    lambda c: self.post(action="purge", titles="|".join(c), forcelinkupdate="1"), chunks), 1):
                if n % 50 == 0 or n == len(chunks):
                    print(f"  purged {min(n * batch, len(titles))}/{len(titles)}", flush=True)

    def push(self, pages: dict[str, str], summary: str, state_path: Path, delay: float, workers: int = 1):
        from concurrent.futures import ThreadPoolExecutor
        state = json.loads(state_path.read_text(encoding="utf-8")) if state_path.exists() else {}
        todo = [t for t, text in pages.items() if state.get(t) != hashlib.sha1(text.encode()).hexdigest()]
        # Templates and site CSS/JS go first and alone: a page saved before its template caches red links.
        site = [t for t in todo if t.startswith(("Template:", "MediaWiki:"))]
        rest = [t for t in todo if t not in set(site)]
        print(f"{len(todo)} of {len(pages)} pages changed since the last push", flush=True)
        done = errors = 0

        def save(title: str, live: dict[str, str]) -> tuple[str, str | None]:
            text = pages[title]
            if live.get(title, "").rstrip() == text.rstrip():
                return title, None
            r = self.post(action="edit", title=title, text=text, summary=summary, bot="1", token=self.csrf, maxlag="5")
            time.sleep(delay)
            return title, None if r.get("edit", {}).get("result") == "Success" else str(r.get("error", r))

        with ThreadPoolExecutor(max_workers=max(1, workers)) as pool:
            for group, parallel in ((site, False), (rest, True)):
                for i in range(0, len(group), 50):
                    batch = group[i:i + 50]
                    live = self.current(batch)
                    results = (pool.map(lambda t: save(t, live), batch) if parallel
                               else (save(t, live) for t in batch))
                    for title, error in results:
                        if error:
                            errors += 1
                            print(f"  FAILED {title}: {error}", flush=True)
                            continue
                        state[title] = hashlib.sha1(pages[title].encode()).hexdigest()
                        done += 1
                    state_path.write_text(json.dumps(state), encoding="utf-8")
                    print(f"  {done}/{len(todo)} pushed, {errors} failed", flush=True)


def main():
    p = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    p.add_argument("--universe", required=True, help="universe display name, e.g. GLMZ")
    p.add_argument("--export-zip", required=True)
    p.add_argument("--interchange")
    p.add_argument("--beats-dir")
    p.add_argument("--book-titles", help="JSON {slug: title}")
    p.add_argument("--theme", help="skin pages from tools/wiki/theme/<name>.css/.js, e.g. cyberspace")
    p.add_argument("--tagged", help="prose --tag-entity-text --out file: entity text carrying <entity> tags")
    p.add_argument("--out", required=True)
    p.add_argument("--phase", choices=["1", "2", "all"], default="all",
                   help="1 = entities the canon books name, 2 = the rest")
    p.add_argument("--repo", nargs="*", help="only these repos")
    p.add_argument("--push", action="store_true")
    p.add_argument("--cred", default=os.path.expandvars(r"%APPDATA%\MindAttic\Wiki\glmz.json"))
    p.add_argument("--delay", type=float, default=0.3, help="seconds between edits")
    p.add_argument("--limit", type=int, help="push at most N pages (a trial run)")
    p.add_argument("--workers", type=int, default=1, help="concurrent page edits (templates always go first, alone)")
    p.add_argument("--purge", action="store_true", help="after pushing (or alone), re-render every article so links "
                   "to pages created later stop showing red")
    args = p.parse_args()
    pages = build(args)
    if args.push or args.purge:
        wiki = Wiki(Path(args.cred))
    if args.push:
        if args.limit:
            keep = [t for t in pages if not t.startswith(("Template:", "MediaWiki:", "Category:", "Main Page"))][:args.limit]
            pages = {t: v for t, v in pages.items() if t in keep or t.startswith(("Template:", "MediaWiki:"))}
        wiki.push(pages, f"prose2wiki: {args.universe} phase {args.phase}",
                                   Path(args.out) / "pushed.json", args.delay, args.workers)
    if args.purge:
        wiki.purge([t for t in pages if not t.startswith(("Template:", "MediaWiki:"))], workers=args.workers)


if __name__ == "__main__":
    main()
