"""The canon books as wiki pages: a book page (cover, blurb, downloads, contents) in the Book namespace and one page per
chapter, whose entity mentions are wiki links. Read by prose2wiki.py; the sync supplies the inputs.

Inputs, per book (tools/wiki/<universe>.books.json: {slug: {"title", "code", "order"}}):
  <books-dir>/<slug>/<CODE>/<CODE> V<n>.md   `prose --export-node --slug <slug> --preview <books-dir>/<slug>` (chapters,
                                              beat markers, <entity> tags), plus .pdf .epub .docx .txt and description.txt
  <books-dir>/<slug>/cover.(jpg|png)          copied by the sync from the publish folder, when the book has a cover
"""
from __future__ import annotations

import hashlib
import json
import re
from dataclasses import dataclass, field
from pathlib import Path

BOOK_NS = "Book"
DOWNLOADS = (("pdf", "PDF"), ("epub", "EPUB"), ("docx", "Word"), ("md", "Markdown"), ("txt", "Plain text"))
PRONOUNS = set("""i me my mine myself you your yours yourself yourselves he him his himself she her hers herself it its
itself we us our ours ourselves they them their theirs themselves who whom whose one someone""".split())
BEAT_MARK = re.compile(r"^<!-- beat:[^>]*-->\s*$", re.M)
TAG_RE = re.compile(r'<entity repo="([^"]*)" guid="([0-9a-fA-F-]{32,36})">(.*?)</entity>', re.S)


@dataclass
class Chapter:
    heading: str
    paras: list[str]
    beats: list[str] = field(default_factory=list)  # beat ids (no dashes) in this chapter, from the export's markers


@dataclass
class Book:
    slug: str
    title: str
    code: str
    order: float
    series: str = ""
    collection: bool = False  # its series is a collection (no reading order inside it), not a numbered series
    subtitle: str = ""
    author: str = ""
    description: str = ""
    chapters: list[Chapter] = field(default_factory=list)
    files: dict[str, Path] = field(default_factory=dict)  # ext -> local file
    cover: Path | None = None
    store_url: str = ""  # its Amazon (KDP) page, when it is published there

    @property
    def page(self) -> str:
        return f"{BOOK_NS}:{self.title}"

    def chapter_page(self, n: int) -> str:
        return f"{self.page}/{n}"


def book_config(path: Path) -> dict[str, dict]:
    """{slug: {title, code, order, series}}; a bare {slug: title} file still works. Keys starting with _ are not books."""
    raw = json.loads(path.read_text(encoding="utf-8"))
    return {slug: (v if isinstance(v, dict) else {"title": v}) for slug, v in raw.items() if not slug.startswith("_")}


def series_list(path: Path) -> list[dict]:
    """The series in the order the main page shows them: [{name, about}] from the books file's _series (PRS-T1054)."""
    raw = json.loads(path.read_text(encoding="utf-8"))
    return [s for s in raw.get("_series") or [] if isinstance(s, dict) and s.get("name")]


BEAT_ID = re.compile(r"<!-- beat:\d+:([0-9a-fA-F-]{32,36}) -->")


def parse_md(text: str, book: Book):
    text = text.replace("\r\n", "\n")
    head, current, waiting = [], None, []  # waiting: beat ids seen before the chapter they open
    for block in re.split(r"\n\s*\n", text):
        ids = [i.replace("-", "").lower() for i in BEAT_ID.findall(block)]
        block = BEAT_MARK.sub("", block).strip()
        if ids:
            (current.beats if current is not None else waiting).extend(ids)
        if not block:
            continue
        if block.startswith("## "):
            heading = block[3:].strip()
            # The export opens with a section named after the book (an epigraph or a song): call it what it is.
            current = Chapter("Opening" if heading.lower() == book.title.lower() else heading, [], waiting)
            waiting = []
            book.chapters.append(current)
            continue
        if current is None and (block.startswith(("# ", "### ")) or re.fullmatch(r"_by (.+)_", block)):
            head.append(block)
            continue
        if current is None:  # a book told in one piece, with no chapter headings
            current = Chapter(book.title, [], waiting)
            waiting = []
            book.chapters.append(current)
        current.paras.append(block)
    for line in head:
        if line.startswith("### "):
            book.subtitle = line[4:].strip()
        elif re.fullmatch(r"_by (.+)_", line):
            book.author = line[4:-1].strip()


def load_books(config: Path, books_dir: Path | None) -> list[Book]:
    books = []
    collections = {s["name"] for s in series_list(config) if s.get("collection")}
    for slug, c in book_config(config).items():
        book = Book(slug, c["title"], c.get("code") or slug.upper(), float(c.get("order") or 99), c.get("series") or "")
        book.collection = book.series in collections
        book.year = c.get("year")  # in-world year, the timeline's anchor until Prose stamps beat dates (PRS-T1069)
        book.start = c.get("start")  # in-world start date where the chronology knows it (GLMZ.Wiki docs/chronology.md)
        folder = books_dir / slug if books_dir else None
        if folder and folder.is_dir():
            md = sorted(folder.glob("*/* V*.md"), key=lambda p: p.stat().st_mtime)
            if md:
                parse_md(md[-1].read_text(encoding="utf-8"), book)
                # The newest file of each format, whatever its version: a partial export (md only, the read gate)
                # sits beside the published edition's pdf/epub/docx/txt that the sync copied in.
                for ext, _ in DOWNLOADS:
                    found = sorted(md[-1].parent.glob(f"* V*.{ext}"), key=lambda p: p.stat().st_mtime)
                    if found:
                        book.files[ext] = found[-1]
                desc = md[-1].parent / "description.txt"
                if desc.exists():
                    book.description = desc.read_text(encoding="utf-8").strip()
            for name in ("cover.jpg", "cover.png"):
                if (folder / name).exists():
                    book.cover = folder / name
                    break
        books.append(book)
    return sorted(books, key=lambda b: (b.order, b.title))


def clean_markdown(path: Path) -> bytes:
    """The download copy of the .md: no beat markers, no entity markup (those are for Prose's own re-import)."""
    text = BEAT_MARK.sub("", path.read_text(encoding="utf-8").replace("\r\n", "\n"))
    while TAG_RE.search(text):
        text = TAG_RE.sub(r"\3", text)
    return (re.sub(r"\n{3,}", "\n\n", text).strip() + "\n").encode("utf-8")


def file_name(universe: str, book: Book, ext: str) -> str:
    return f"{universe} {book.title} cover.{ext}" if ext in ("jpg", "png") else f"{universe} {book.title}.{ext}"


def uploads(universe: str, books: list[Book]) -> list[tuple[str, bytes, str]]:
    """(wiki file name, content, description) for every cover and download."""
    out = []
    for b in books:
        if b.cover:
            out.append((file_name(universe, b, b.cover.suffix[1:].lower()), b.cover.read_bytes(),
                        f"Cover of ''{b.title}'' ({universe})."))
        for ext, label in DOWNLOADS:
            if ext in b.files:
                data = clean_markdown(b.files[ext]) if ext == "md" else b.files[ext].read_bytes()
                out.append((file_name(universe, b, ext), data, f"''{b.title}'' ({universe}), {label} edition."))
    return out


def sha1(data: bytes) -> str:
    return hashlib.sha1(data).hexdigest()


def size_label(n: int) -> str:
    return f"{n / 1048576:.1f} MB" if n >= 1048576 else f"{max(1, n // 1024)} KB"


class BookRenderer:
    def __init__(self, renderer, escape, universe: str, entities: dict):
        self.r, self.escape, self.universe, self.entities = renderer, escape, universe, entities

    def para(self, text: str) -> str:
        """Every named mention is a link; pronouns and "not an entity" marks stay plain text."""
        out, pos = [], 0
        for m in TAG_RE.finditer(text):
            out.append(self.escape(text[pos:m.start()]))
            repo, guid, inner = m.group(1), m.group(2).replace("-", "").lower(), m.group(3)
            target = self.entities.get(guid)
            plain = TAG_RE.sub(r"\3", inner)
            if repo == "none" or target is None or plain.strip().lower() in PRONOUNS:
                out.append(self.escape(plain))
            else:
                self.r.linked.add(guid)
                out.append(self.r.link(target, plain))
            pos = m.end()
        out.append(self.escape(text[pos:]))
        return "".join(out)

    def cover(self, b: Book, width: int) -> str:
        if b.cover:
            return f"[[File:{file_name(self.universe, b, b.cover.suffix[1:].lower())}|{width}px|link={b.page}|alt={self.escape(b.title)}]]"
        return (f'<div class="prose-cover-blank" style="width:{width}px;height:{int(width * 1.5)}px">'
                f'<span class="prose-cover-blank-u">{self.universe}</span>'
                f'<span class="prose-cover-blank-t">[[{b.page}|{self.escape(b.title)}]]</span></div>')

    def book_page(self, b: Book, index: int, books: list[Book], sizes: dict[str, int], mentions: int) -> str:
        e = self.escape
        lines = [f"{{{{DISPLAYTITLE:<i>{e(b.title)}</i>}}}}", '<div class="prose-bookpage">',
                 f'<div class="prose-bookpage-cover">{self.cover(b, 260)}</div>', '<div class="prose-bookpage-info">']
        if b.subtitle:
            lines.append(f'<div class="prose-book-subtitle">{e(b.subtitle)}</div>')
        meta = f"Book {index} of {len(books)} in {e(self.universe)} story order"
        if b.series:
            mates = [x for x in books if x.series == b.series]
            meta = (f"[[:Category:{b.series}|{e(b.series)}]]"
                    + (f", book {mates.index(b) + 1} of {len(mates)}" if len(mates) > 1 and not b.collection else "")
                    + " &middot; " + meta)
        lines.append(f'<div class="prose-book-meta">{meta}' + (f" &middot; by {e(b.author)}" if b.author else "") + "</div>")
        for para in [p.strip() for p in b.description.split("\n") if p.strip()]:
            lines += ["", e(para)]
        lines.append("")
        if b.chapters or b.store_url:
            buttons = []
            if b.chapters:
                buttons.append(f'<span class="prose-read">[[{b.chapter_page(1)}|Start reading]]</span>')
            if b.store_url:
                buttons.append(f'<span class="prose-read prose-store">[{b.store_url} On Amazon Kindle]</span>')
            # prose-book-slug: the theme's "Mark as read" button needs the book's id (spoilers, PRS-T1068)
            lines.append('<div class="prose-actions">' + " ".join(buttons) +
                         f'<span class="prose-book-slug">{b.slug}</span></div>')
        if b.files:
            links = " ".join(f'<span class="prose-dl">[[Media:{file_name(self.universe, b, ext)}|{label}]]'
                             f'<small>{size_label(sizes[ext])}</small></span>'
                             for ext, label in DOWNLOADS if ext in b.files and ext in sizes)
            lines.append(f'<div class="prose-downloads">Download: {links}</div>')
        lines += ["</div>", "</div>", ""]
        if b.chapters:
            lines.append("== Contents ==")
            lines.append('<div class="prose-toc">')
            lines += [f"# [[{b.chapter_page(n)}|{e(c.heading)}]]" for n, c in enumerate(b.chapters, 1)]
            lines += ["</div>", ""]
        if b.chapters:  # the story checks (wikistory.py): how the book moves and what it sets up
            craft = [f"[[{b.page}/Pacing|Pacing, chapter by chapter]]"]
            if getattr(b, "has_plants", False):
                craft.append(f"[[{b.page}/Plants|Plants and payoffs]] (spoilers)")
            lines += ['<div class="prose-craft">How it is built: ' + " &middot; ".join(craft) + "</div>", ""]
        lines.append("== In this book ==")
        lines.append(f"''{e(b.title)}'' names {mentions} entries of the {e(self.universe)} wiki: "
                     f"[[:Category:Appears in {b.title}|see them all]].")
        nav = []
        if index > 1:
            prev = books[index - 2]
            nav.append(f"Previous book: [[{prev.page}|<i>{e(prev.title)}</i>]]")
        if index < len(books):
            nxt = books[index]
            nav.append(f"Next book: [[{nxt.page}|<i>{e(nxt.title)}</i>]]")
        if nav:
            lines += ["", '<div class="prose-booknav">' + " &middot; ".join(nav) + "</div>"]
        lines += ["", "[[Category:Books]]"]
        if b.series:
            lines.append(f"[[Category:{b.series}|{b.order:08.3f}]]")  # sorted in story order within the series
        return "\n".join(lines) + "\n"

    def chapter_page(self, b: Book, n: int) -> str:
        e, c = self.escape, b.chapters[n - 1]
        prev_link = (f'<span class="prose-prev">[[{b.chapter_page(n - 1)}|&larr; {e(b.chapters[n - 2].heading)}]]</span>'
                     if n > 1 else f'<span class="prose-prev">[[{b.page}|&larr; About the book]]</span>')
        next_link = (f'<span class="prose-next">[[{b.chapter_page(n + 1)}|{e(b.chapters[n].heading)} &rarr;]]</span>'
                     if n < len(b.chapters) else f'<span class="prose-next">[[{b.page}|The end &middot; back to the book]]</span>')
        nav = (f'<div class="prose-chapnav">{prev_link}<span class="prose-contents">[[{b.page}|<i>{e(b.title)}</i>]] '
               f'&middot; {n} of {len(b.chapters)}</span>{next_link}</div>')
        body = "\n\n".join(self.para(p) for p in c.paras)
        return (f"{{{{DISPLAYTITLE:{e(c.heading)}}}}}\n__NOTOC__ __NOEDITSECTION__\n{nav}\n"
                f'<div class="prose-book" data-book="{e(b.title)}">\n<div class="prose-chapter-title">{e(c.heading)}</div>\n\n'
                f"{body}\n</div>\n{nav}\n[[Category:Chapters of {b.title}]]\n")

    def pages(self, books: list[Book], sizes: dict[str, dict[str, int]]) -> dict[str, str]:
        pages = {"Category:Books": f"Every {self.universe} book, in story order: see [[Main Page]].\n"}
        for i, b in enumerate(books, 1):
            mentions = sum(1 for ent in self.entities.values() if b.slug in ent.books)
            pages[b.page] = self.book_page(b, i, books, sizes.get(b.slug, {}), mentions)
            for n in range(1, len(b.chapters) + 1):
                pages[b.chapter_page(n)] = self.chapter_page(b, n)
            pages[f"Category:Chapters of {b.title}"] = f"__HIDDENCAT__\nThe chapters of [[{b.page}|''{b.title}'']].\n"
        return pages
