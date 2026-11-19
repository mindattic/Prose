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

sys.path.insert(0, str(Path(__file__).parent))
import wikibooks  # noqa: E402

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


def same_text(tagged, current) -> bool:
    """True when a tagged value, with its <entity> markup removed, equals the current canon value."""
    def strip(x):
        if isinstance(x, str):
            return TAG_RE.sub(r"\3", x)
        if isinstance(x, list):
            return [strip(i) for i in x]
        if isinstance(x, dict):
            return {k: strip(i) for k, i in x.items()}
        return x
    return json.dumps(strip(tagged), ensure_ascii=False, sort_keys=True) == json.dumps(current, ensure_ascii=False, sort_keys=True)


def overlay_tagged(path: Path, entities: dict[str, Entity], repair: Repair):
    """Apply `prose --tag-entity-text --out` output: entity text with <entity> markup, so links come from canon tags."""
    stale_count = [0]
    try:
        _overlay(path, entities, repair, stale_count)
    finally:
        if stale_count[0]:
            repair.add("tagged fields older than canon (ignored; re-run with -Retag)", count=stale_count[0])


def _overlay(path: Path, entities: dict[str, Entity], repair: Repair, stale_count: list[int]):
    for guid, v in json.loads(path.read_text(encoding="utf-8")).items():
        if guid == "_wordLikeNames":
            for word in v:
                repair.add("one-word name or alias used more as an ordinary word than as a name (tagged mid-sentence only)", text=word)
            continue
        ent = entities.get(norm(guid))
        if ent is None:
            stale_count[0] += 1  # merged or deleted since the tags were made
            continue
        # A tagged field is used only while it still says what canon says: an edit made after the tagging run
        # (a removed relationship, a fixed typo) must not be undone by an older tagged copy.
        for key, value in (v.get("record") or {}).items():
            if same_text(value, ent.record.get(key)):
                ent.record[key] = value
            else:
                stale_count[0] += 1
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
                    title = book_titles.get(book, book.upper())
                    lines.append(f"* [[Book:{title}|<i>{escape(title)}</i>]] ({ent.books[book]} mentions)")
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
    # The wiki is the media store (PRS-T1053): File:<page title>.png/.jpg/.webp, once uploaded, is the entry's picture.
    "Template:Infobox entity": """<includeonly><div class="prose-infobox"><div class="prose-infobox-title">{{{title}}}</div>{{#ifexist:File:{{PAGENAME}}.png|<div class="prose-infobox-image">[[File:{{PAGENAME}}.png|300px|{{{title}}}]]</div>|{{#ifexist:File:{{PAGENAME}}.jpg|<div class="prose-infobox-image">[[File:{{PAGENAME}}.jpg|300px|{{{title}}}]]</div>|{{#ifexist:File:{{PAGENAME}}.webp|<div class="prose-infobox-image">[[File:{{PAGENAME}}.webp|300px|{{{title}}}]]</div>}}}}}}<div class="prose-infobox-repo">[[:Category:{{{repo}}}|{{{repo}}}]]</div><div class="prose-infobox-aliases">{{{aliases|}}}</div><table class="prose-infobox-rows"></includeonly><noinclude>Opens an entity infobox. Rows follow as {{tl|Infobox row}}, closed by {{tl|Infobox end}}. Generated by prose2wiki.</noinclude>""",
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
        pages[f"Category:Appears in {title}"] = f"Entities named in the prose of [[Book:{title}|<i>{escape(title)}</i>]].\n"
    return pages


def main_page(universe: str, entities: dict[str, Entity], shelf: list, bookr, intro: str, logo: str | None,
              relations: int, series: list[dict] | None = None) -> str:
    """The front door: the logo, the series introduction, every book in story order, every repository."""
    repos = collections.Counter(e.repo for e in entities.values())
    canon = collections.Counter(e.repo for e in entities.values() if e.books)
    chapters = sum(len(b.chapters) for b in shelf)
    stat = lambda n, label: f'<div class="prose-stat"><b>{n:,}</b><span>{label}</span></div>'
    lines = ["__NOTOC__ __NOEDITSECTION__", "{{DISPLAYTITLE:<span></span>}}", '<div class="prose-hero">']
    if logo:
        lines.append(f'<div class="prose-hero-logo">[[File:{logo}|300px|link=|alt={escape(universe)}]]</div>')
    lines += ['<div class="prose-hero-text">', intro.strip(), "</div>", '<div class="prose-stats">',
              stat(len(shelf), "books"), stat(chapters, "chapters"), stat(len(entities), "entries"),
              stat(relations, "relationships"), stat(len(repos), "repositories"), "</div>"]
    if shelf and shelf[0].chapters:
        lines.append(f'<div class="prose-read">[[{shelf[0].chapter_page(1)}|Start at the beginning: '
                     f'<i>{escape(shelf[0].title)}</i>]]</div>')
    lines += ["</div>", "", "== The books ==",
              f"Every {escape(universe)} book, free to read here, by series. The numbers are story order: read them "
              "in that order to meet everything as it happens. Open one for its blurb, its chapters and the files to "
              "download."]
    number = {b.slug: i for i, b in enumerate(shelf, 1)}
    named = [s for s in series or [] if any(b.series == s["name"] for b in shelf)]
    groups = [(s["name"], s.get("about") or "", [b for b in shelf if b.series == s["name"]]) for s in named]
    rest = [b for b in shelf if b.series not in {s["name"] for s in named}]
    if rest:
        groups.append(("More books" if groups else "", "", rest))
    for name, about, books in groups:
        if name:
            lines += ["", f"=== [[:Category:{name}|{escape(name)}]] ==="]
            if about:
                lines.append(f'<div class="prose-series-about">{escape(about)}</div>')
        lines.append('<div class="prose-shelf">')
        for b in books:
            lines.append(f'<div class="prose-shelf-item">{bookr.cover(b, 150)}<div class="prose-shelf-n">{number[b.slug]}</div>'
                         f'<div class="prose-shelf-title">[[{b.page}|{escape(b.title)}]]</div></div>')
        lines.append("</div>")
    lines += ["", "== The repositories ==",
              f"Everything Prose knows about the {escape(universe)}: every person, place, faction and thing the books "
              "draw on, each with its relationships. Bold numbers count the entries the books name.",
              '<div class="prose-repos">']
    for r, n in sorted(repos.items(), key=lambda x: (-x[1], repo_label(x[0]))):
        lines.append(f'<div class="prose-repo">[[:Category:{repo_label(r)}|{repo_label(r)}]]'
                     f'<span><b>{canon.get(r, 0):,}</b> in the books &middot; {n:,} in all</span></div>')
    lines += ["</div>", "", "== Wander ==",
              '<div class="prose-wander">[[:Category:Canon|Everything the books name]] '
              "[[:Category:Background|The deep background]] [[Special:Random|A random entry]] "
              "[[Special:AllPages|Every page]]</div>"]
    return "\n".join(lines) + "\n"


def build(args) -> dict[str, str]:
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    repair = Repair()
    entities = load_entities(Path(args.export_zip), Path(args.interchange) if args.interchange else None, repair)
    if args.tagged:
        overlay_tagged(Path(args.tagged), entities, repair)
    books = load_presence(Path(args.beats_dir) if args.beats_dir else None, entities, repair)
    config = wikibooks.book_config(Path(args.book_titles)) if args.book_titles else {}
    book_titles = {slug: c["title"] for slug, c in config.items()}
    order = {slug: float(c.get("order") or 99) for slug, c in config.items()}
    books.sort(key=lambda s: (order.get(s, 99), book_titles.get(s, s)))
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
    # The books themselves: a page per book and per chapter, with their covers and downloads uploaded beside them.
    shelf = wikibooks.load_books(Path(args.book_titles), Path(args.books_dir) if args.books_dir else None) \
        if args.book_titles else []
    if getattr(args, "store", None) and Path(args.store).exists():
        store = json.loads(Path(args.store).read_text(encoding="utf-8-sig"))
        for b in shelf:
            s = store.get(b.slug) or store.get(b.code.lower()) or {}
            url = s.get("url") or ""
            if url and not re.match(r"https://(www\.)?amazon\.[a-z.]+/", url):
                repair.add("PublishUrl is not a store page (the wiki links the ASIN instead)", book=b.slug, url=url)
                url = ""
            # The ASIN is the stable address of the store page; a recorded product URL is the fallback.
            b.store_url = f"https://www.amazon.com/dp/{s['asin']}" if s.get("asin") else url
    files = wikibooks.uploads(args.universe, shelf)
    sizes = collections.defaultdict(dict)
    for b in shelf:
        for ext, _ in wikibooks.DOWNLOADS:
            name = wikibooks.file_name(args.universe, b, ext)
            sizes[b.slug].update({ext: len(d) for n, d, _ in files if n == name})
    bookr = wikibooks.BookRenderer(renderer, escape, args.universe, entities)
    pages.update(bookr.pages(shelf, sizes))
    for b in shelf:
        if b.title not in pages:
            pages[b.title] = f"#REDIRECT [[{b.page}]]\n"
        if not b.chapters:
            repair.add("book with no exported text (not exported yet, or unread beats)", book=b.slug)
    logo = None
    if args.logo:
        logo = f"{args.universe} logo{Path(args.logo).suffix.lower()}"
        files.append((logo, Path(args.logo).read_bytes(), f"The {args.universe} logo."))
    site = Path(args.site_dir) if getattr(args, "site_dir", None) else Path(__file__).parent  # the <UNIVERSE>.Wiki repo
    intro_path = site / f"{args.universe.lower()}.intro.wiki"
    intro = intro_path.read_text(encoding="utf-8") if intro_path.exists() else \
        f"Welcome to the '''{args.universe} Wiki''', generated from the Prose canon."
    relations = sum(1 for e in entities.values() for rel in e.record.get("_relations") or []
                    if slug_key(rel.get("to") or "") in renderer.by_slug) \
        + sum(len(e.record["relationships"]) for e in entities.values() if isinstance(e.record.get("relationships"), list))
    series = wikibooks.series_list(Path(args.book_titles)) if args.book_titles else []
    pages["Main Page"] = main_page(args.universe, entities, shelf, bookr, intro, logo, relations, series)
    for s in series:  # a category page per series: its blurb; the books list themselves in story order
        books_in = [b for b in shelf if b.series == s["name"]]
        if books_in:
            pages[f"Category:{s['name']}"] = (
                f"'''{escape(s['name'])}''': {escape(s.get('about') or '')} {len(books_in)} book"
                f"{'s' if len(books_in) != 1 else ''} of the {escape(args.universe)} series, in story order.\n\n"
                "[[Category:Series]]\n")
    if series:
        pages["Category:Series"] = f"The series of {escape(args.universe)} books.\n"
    (out / "files.json").write_text(json.dumps({n: wikibooks.sha1(d) for n, d, _ in files}, indent=0), encoding="utf-8")
    build.files = files
    # An entity renamed since the last build keeps its old page as a redirect, so links and bookmarks still land.
    previous = out / "pages.json"
    stale: list[str] = []
    if previous.exists():
        marker = re.compile(r"<!-- prose guid ([0-9a-f-]{36}) -->")
        now = {m.group(1): t for t, text in pages.items() if (m := marker.search(text))}
        for old_title, text in json.loads(previous.read_text(encoding="utf-8")).items():
            m = marker.search(text)
            if m and old_title not in pages and m.group(1) in now:
                pages[old_title] = f"#REDIRECT [[{now[m.group(1)]}]]\n"
            elif m and old_title not in pages:
                stale.append(old_title)  # its entity is gone (merged or deleted): the page goes too
    (out / "stale.json").write_text(json.dumps(stale, ensure_ascii=False), encoding="utf-8")
    if args.theme:
        theme = site / "theme"
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
        self.cred = json.loads(cred_path.read_text(encoding="utf-8-sig"))  # a Vault file saved by PowerShell 5 has a BOM
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

    def sync_files(self, files: list[tuple[str, bytes, str]]):
        """Uploads each (name, content, description) whose content the wiki doesn't already hold."""
        live = {}
        names = [n for n, _, _ in files]
        for i in range(0, len(names), 50):
            r = self.get(action="query", prop="imageinfo", iiprop="sha1", titles="|".join(f"File:{n}" for n in names[i:i + 50]))
            for p in r.get("query", {}).get("pages", []):
                if p.get("imageinfo"):
                    live[p["title"][5:]] = p["imageinfo"][0]["sha1"]
            for n in r.get("query", {}).get("normalized", []):  # "File:x" -> "File:X"
                if n["to"][5:] in live:
                    live[n["from"][5:]] = live[n["to"][5:]]
        todo = [(n, d, desc) for n, d, desc in files if live.get(n) != wikibooks.sha1(d)]
        print(f"{len(todo)} of {len(files)} files changed since the last upload", flush=True)
        failed = 0
        for name, data, desc in todo:
            for attempt in range(4):
                try:
                    r = self.s.post(self.api, data={"action": "upload", "filename": name, "comment": "prose2wiki",
                                                    "text": desc, "ignorewarnings": "1", "token": self.csrf,
                                                    "format": "json", "formatversion": "2"},
                                    files={"file": (name, data)}, timeout=600).json()
                    break
                except Exception as e:
                    print(f"  retry {name}: {e}", flush=True)
                    time.sleep(2 ** attempt)
            else:
                r = {"error": "network"}
            ok = r.get("upload", {}).get("result") == "Success"
            failed += not ok
            print(f"  {'uploaded' if ok else 'FAILED'} {name} ({wikibooks.size_label(len(data))})"
                  + ("" if ok else f": {r.get('error', r)}"), flush=True)
        if failed:
            print(f"  {failed} upload(s) failed", flush=True)

    def delete(self, titles: list[str], reason: str):
        for title in titles:
            r = self.post(action="delete", title=title, reason=reason, token=self.csrf)
            ok = "delete" in r or r.get("error", {}).get("code") == "missingtitle"
            print(f"  {'deleted' if ok else 'FAILED to delete'} {title}", flush=True)

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
        # Pages a person edited on the wiki whose proposals wikireconcile.py has not had decided yet: the sync does
        # not write over them (PRS-T1048). held.json maps title -> the held revision id.
        held_path = state_path.parent / "held.json"
        held = json.loads(held_path.read_text(encoding="utf-8")) if held_path.exists() else {}
        if held:
            print(f"{len(held)} page(s) held for a decision on people's edits (wikireconcile.py)", flush=True)
        todo = [t for t, text in pages.items() if t not in held and state.get(t) != hashlib.sha1(text.encode()).hexdigest()]
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
    p.add_argument("--book-titles", help="JSON {slug: {title, code, order}} (or {slug: title}): the canon books")
    p.add_argument("--books-dir", help="per-book prose --export-node --preview output (and cover.jpg), one folder per slug")
    p.add_argument("--logo", help="the universe's logo image, shown on the main page")
    p.add_argument("--site-dir", help="the wiki's site repo (<UNIVERSE>.Wiki): theme/, <universe>.intro.wiki")
    p.add_argument("--store", help="JSON {slug: {url, asin}}: where each book is on sale (Node.PublishUrl / Asin)")
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
    if args.push and getattr(build, "files", None):
        wiki.sync_files(build.files)  # before the pages, so their File: and Media: links resolve
    if args.push:
        if args.limit:
            keep = [t for t in pages if not t.startswith(("Template:", "MediaWiki:", "Category:", "Main Page"))][:args.limit]
            pages = {t: v for t, v in pages.items() if t in keep or t.startswith(("Template:", "MediaWiki:"))}
        wiki.push(pages, f"prose2wiki: {args.universe} phase {args.phase}",
                                   Path(args.out) / "pushed.json", args.delay, args.workers)
    if args.push:
        stale = json.loads((Path(args.out) / "stale.json").read_text(encoding="utf-8"))
        if stale:
            wiki.delete(stale, f"prose2wiki: {args.universe} entity no longer exists (merged or deleted)")
    if args.purge:
        wiki.purge([t for t in pages if not t.startswith(("Template:", "MediaWiki:"))], workers=args.workers)


if __name__ == "__main__":
    main()
