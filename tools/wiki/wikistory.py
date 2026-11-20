"""Story checks on the wiki (PRS-T1067): what Prose knows about how the books are built, shown where readers and the
author can see it, plus one graph file both sides read.

  Book:<Title>/Plants   every registered plant and its payoff, by chapter (Prose's PlantPayoffs)
  Book:<Title>/Pacing   chapter by chapter: length, beats, who and what first appears, plants set and paid off
  GLMZ Wiki:Continuity  the continuity store's open contradictions, by entry, with the chapters that disagree
  File:<U> graph.json   entities, books and chapters with every edge: Prose relations and relationships, book and
                        chapter appearances, plants/payoffs, contradictions (the shared graph of both systems)

Inputs come from the sync (sync-wiki.ps1 -> <state>\\story\\): plants.json ({slug: [PlantPayoff]}) and
contradictions.txt (`prose --continuity contradictions`).
"""
from __future__ import annotations

import collections
import json
import re
from pathlib import Path

TAG_RE = re.compile(r'<entity repo="([^"]*)" guid="([0-9a-fA-F-]{32,36})">(.*?)</entity>', re.S)
CONTRA_HEAD = re.compile(r"^\[(.+?)\]\s+(\S.*?)\s*$")
CONTRA_LINE = re.compile(r'^\s+([AB])\s+(claim-\w+)\s+\S+\s+"(.*)"\s+\((.*)\)\s*$')


def norm(g: str) -> str:
    return (g or "").replace("-", "").lower()


def load_plants(story: Path) -> dict[str, list[dict]]:
    path = story / "plants.json"
    if not path.exists():
        return {}
    raw = json.loads(path.read_text(encoding="utf-8-sig"))
    return {slug: (rows if isinstance(rows, list) else [rows]) for slug, rows in raw.items() if rows}


def load_contradictions(story: Path) -> list[dict]:
    path = story / "contradictions.txt"
    if not path.exists():
        return []
    out, cur = [], None
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        m = CONTRA_HEAD.match(line)
        if m:
            cur = {"entity": m.group(1), "predicate": m.group(2), "claims": []}
            out.append(cur)
            continue
        m = CONTRA_LINE.match(line)
        if m and cur is not None:
            cur["claims"].append({"side": m.group(1), "claim": m.group(2), "value": m.group(3), "where": m.group(4)})
    return [c for c in out if len(c["claims"]) >= 2]


class Story:
    def __init__(self, universe, shelf, entities, renderer, resolver, escape, story: Path | None):
        self.u, self.shelf, self.entities, self.r, self.resolver, self.e = universe, shelf, entities, renderer, resolver, escape
        self.plants = load_plants(story) if story else {}
        self.contradictions = load_contradictions(story) if story else []
        self.story_dir = story
        # (book slug, beat id) -> chapter number: a beat can sit in more than one book
        self.where = {(b.slug, bid): n for b in shelf for n, c in enumerate(b.chapters, 1) for bid in c.beats}

    # ---- helpers -------------------------------------------------------------------------------------------------
    def chapter_link(self, b, n: int | None) -> str:
        if not n:
            return "&mdash;"
        return f"[[{b.chapter_page(n)}|{n}. {self.e(b.chapters[n - 1].heading)}]]"

    def chapter_of(self, b, beat_id) -> int | None:
        return self.where.get((b.slug, norm(beat_id)))

    def entity_link(self, name: str) -> str:
        ent = self.resolver.by_name(name)
        return self.r.link(ent, name) if ent else self.e(name)

    # ---- pages ---------------------------------------------------------------------------------------------------
    def pages(self) -> dict[str, str]:
        pages = {}
        for b in self.shelf:
            if not b.chapters:
                continue
            rows = self.plants.get(b.slug) or []
            if rows:
                pages[f"{b.page}/Plants"] = self.plants_page(b, rows)
            pages[f"{b.page}/Pacing"] = self.pacing_page(b, rows)
        notes = self.reading_notes(self.story_dir)
        pages[f"Project:Continuity"] = self.continuity_page().replace("[[Category:Maintenance]]", notes + "\n[[Category:Maintenance]]")
        return pages

    def plants_page(self, b, rows) -> str:
        e = self.e
        rows = sorted(rows, key=lambda r: (self.chapter_of(b, r.get("PlantBeatId")) or 999, r.get("SortKey") or 0))
        lines = [f"{{{{DISPLAYTITLE:Plants and payoffs in <i>{e(b.title)}</i>}}}}", "__NOTOC__",
                 f"'''Spoilers.''' Every plant ''[[{b.page}|{e(b.title)}]]'' sets up and where it pays off, as Prose has "
                 f"them registered: {len(rows)} in all. A plant is planted so a re-reader finds it waiting; the payoff "
                 "is what they get.", "",
                 f'<div class="prose-spoiler" data-book="{b.slug}" data-title="{e(b.title)}">',
                 '{| class="wikitable prose-plants"', "! Planted !! What is seeded !! Paid off !! What the reader gets !! Kind"]
        unplaced = 0
        for r in rows:
            pn, qn = self.chapter_of(b, r.get("PlantBeatId")), self.chapter_of(b, r.get("PayoffBeatId"))
            unplaced += not pn or not qn
            lines += ["|-", f"| {self.chapter_link(b, pn)} || {e(r.get('PlantDescription') or '')} || "
                            f"{self.chapter_link(b, qn)} || {e(r.get('PayoffDescription') or '')} || {e(r.get('Category') or '')}"]
        lines += ["|}", "</div>"]
        if unplaced:
            lines += ["", f"''{unplaced} of these point at a beat the current text no longer has (moved or cut): "
                          "Prose's registry needs them re-pointed.''"]
        lines += ["", f"[[Category:Plants and payoffs]]"]
        return "\n".join(lines) + "\n"

    def pacing_page(self, b, plants) -> str:
        e = self.e
        set_in, paid_in = collections.Counter(), collections.Counter()
        for r in plants:
            if (n := self.chapter_of(b, r.get("PlantBeatId"))):
                set_in[n] += 1
            if (n := self.chapter_of(b, r.get("PayoffBeatId"))):
                paid_in[n] += 1
        seen: set[str] = set()
        stats = []
        for n, c in enumerate(b.chapters, 1):
            text = "\n".join(c.paras)
            words = len(TAG_RE.sub(r"\3", TAG_RE.sub(r"\3", text)).split())
            new = []
            for repo, g, inner in TAG_RE.findall(text):
                g = norm(g)
                if repo != "none" and g in self.entities and g not in seen:
                    seen.add(g)
                    new.append(self.entities[g])
            stats.append((n, c, words, len(c.beats), new))
        total = sum(s[2] for s in stats) or 1
        longest = max(s[2] for s in stats) or 1
        lines = [f"{{{{DISPLAYTITLE:Pacing of <i>{e(b.title)}</i>}}}}", "__NOTOC__",
                 f"How ''[[{b.page}|{e(b.title)}]]'' moves, chapter by chapter: {len(stats)} chapters, {total:,} words. "
                 "The bar is the chapter's length against the longest; ''First appear'' counts the entries the book "
                 "names for the first time there; plants set and paid off come from [[" + b.page + "/Plants|the plant "
                 "registry]].", "",
                 '{| class="wikitable prose-pacing"', "! Chapter !! Words !! !! Beats !! First appear !! Plants set !! Paid off"]
        for n, c, words, beats, new in stats:
            bar = f'<div class="prose-bar" style="width:{max(2, round(words / longest * 100))}%"></div>'
            names = ", ".join(self.r.link(x) for x in new[:6]) + (f" +{len(new) - 6}" if len(new) > 6 else "")
            lines += ["|-", f"| {self.chapter_link(b, n)} || {words:,} || {bar} || {beats} || "
                            f"{len(new)}{': ' + names if new else ''} || {set_in.get(n, 0) or ''} || {paid_in.get(n, 0) or ''}"]
        lines += ["|}", "", "[[Category:Pacing]]"]
        return "\n".join(lines) + "\n"

    def reading_notes(self, story: Path | None) -> str:
        """Open read-notes per book (defects and questions filed while reading for the read gate), each linked to its
        chapter: the worklist of contradictions to fix in Prose."""
        if not story or not (story / "notes").is_dir():
            return ""
        note = re.compile(r"^\s*\[(\d+)\]\s+#(\d+)\s+(\w+)\s+(\w+)(\s+\(beat changed since\))?\s+(.*?)\s+\(([0-9a-f-]{36})\)\s*$")
        e, out, total = self.e, [], collections.Counter()
        for b in sorted((x for x in self.shelf if x.chapters), key=lambda x: x.order):
            path = story / "notes" / f"{b.slug}.txt"
            if not path.exists():
                continue
            pos_chapter, k = {}, 0
            for n, c in enumerate(b.chapters, 1):
                for _ in c.beats:
                    k += 1
                    pos_chapter[k] = n
            rows = [m for m in (note.match(l) for l in path.read_text(encoding="utf-8-sig").splitlines()) if m]
            if not rows:
                continue
            rows.sort(key=lambda m: (m.group(3) != "defect", int(m.group(1))))
            out.append(f"=== [[{b.page}|{e(b.title)}]] ===")
            for m in rows:
                total[m.group(3)] += 1
                ch = pos_chapter.get(int(m.group(1)))
                where = self.chapter_link(b, ch) if ch else f"position {m.group(1)}"
                out.append(f"* '''{e(m.group(3))}''' &middot; beat #{m.group(2)} in {where}: "
                           f'<span class="prose-spoiler" data-book="{b.slug}" data-title="{e(b.title)}">{e(m.group(6))}</span>')
            out.append("")
        if not out:
            return ""
        head = [f"== Found by reading ({total['defect']} defects, {total['question']} questions) ==",
                "Filed by the readers who re-read every book for Prose's read gate. A '''defect''' beat stays unread (the book "
                "cannot be exported) until its text or a record is fixed; a '''question''' was read but needs a decision.", ""]
        return "\n".join(head + out)

    def continuity_page(self) -> str:
        e = self.e
        by_entity = collections.defaultdict(list)
        for c in self.contradictions:
            by_entity[c["entity"]].append(c)
        lines = ["__NOEDITSECTION__",
                 f"Where Prose's continuity store says two different things about the same fact: "
                 f"{len(self.contradictions)} open contradictions across {len(by_entity)} entries, refreshed by every "
                 "sync. Each is resolved in Prose (`prose --continuity resolve`), and leaves this page on the next sync.", ""]
        for name in sorted(by_entity, key=str.lower):
            lines.append(f"== {self.entity_link(name)} ==")
            for c in by_entity[name]:
                vals = " &nbsp;vs&nbsp; ".join(f"'''{e(x['value'])}''' <small>({e(x['where'])})</small>" for x in c["claims"])
                lines.append(f"* ''{e(c['predicate'].replace('_', ' '))}'': {vals}")
            lines.append("")
        lines.append("[[Category:Maintenance]]")
        return "\n".join(lines) + "\n"

    # ---- the timeline (PRS-T1069) --------------------------------------------------------------------------------
    def timeline(self, story: Path | None) -> dict:
        """Events in in-world order: every chapter of every book (dated from Prose's Beat.InWorldDate when the book is
        stamped; otherwise placed by story order between its dated neighbours and marked approximate), and the
        records' own dated backstory ("2162 - Born in ..."). The Timeline page's script draws it."""
        import datetime as dt
        inworld = {}
        if story and (story / "inworld").is_dir():
            for f in (story / "inworld").glob("*.json"):
                try:
                    inworld[f.stem] = {norm(r["beatId"]): r.get("inWorldDate") for r in json.loads(f.read_text(encoding="utf-8-sig"))}
                except (ValueError, KeyError):
                    pass
        def parse(s):
            try:
                return dt.datetime.fromisoformat(str(s).replace("Z", "")) if s else None
            except ValueError:
                return None
        books = sorted([b for b in self.shelf if b.chapters], key=lambda b: b.order)
        dated: dict[str, list] = {}
        for b in books:
            dates = inworld.get(b.slug) or {}
            per = []
            for c in b.chapters:
                ds = [d for d in (parse(dates.get(x)) for x in c.beats) if d]
                per.append((min(ds), max(ds)) if ds else None)
            if any(per):
                # chapters with no dated beat take their neighbours' dates
                known = [i for i, p in enumerate(per) if p]
                for i, p in enumerate(per):
                    if not p:
                        j = min(known, key=lambda k: abs(k - i))
                        per[i] = (per[j][1], per[j][1]) if j < i else (per[j][0], per[j][0])
                dated[b.slug] = per
        # Dated books stamped from the same anchor (1 January of their year, until Prose has real start dates) would all
        # begin the same day and interleave: a later book in story order starts after the earlier one ends instead,
        # keeping every chapter's own spacing.
        last_end = None
        for b in books:
            per = dated.get(b.slug)
            if not per:
                continue
            start = min(p[0] for p in per)
            if last_end and start <= last_end and start.year == last_end.year:
                shift = (last_end - start) + dt.timedelta(days=1)
                dated[b.slug] = per = [(s + shift, e + shift) for s, e in per]
                b._shifted = True
            last_end = max(p[1] for p in per)
        # Undated books: between the dated books around them in story order (or a year apart when none are dated).
        spans = {s: (p[0][0], p[-1][1]) for s, p in dated.items()}
        base = dt.datetime(2217, 1, 1)
        for i, b in enumerate(books):
            if b.slug in dated:
                continue
            year = getattr(b, "year", None)
            if year:  # the book's in-world year (books file); books of one year keep their story order inside it
                same = [x for x in books if getattr(x, "year", None) == year and x.slug not in dated]
                k = same.index(b)
                start = dt.datetime(int(year), 1, 1) + dt.timedelta(days=int(330 * k / max(1, len(same))))
                end = start + dt.timedelta(days=int(330 / max(1, len(same))) - 1)
            else:
                prev = next((spans[x.slug] for x in reversed(books[:i]) if x.slug in spans), None)
                nxt = next((spans[x.slug] for x in books[i + 1:] if x.slug in spans), None)
                start = prev[1] + dt.timedelta(days=1) if prev else (nxt[0] - dt.timedelta(days=60) if nxt else base + dt.timedelta(days=200 * i))
                end = min(start + dt.timedelta(days=14), nxt[0]) if nxt and nxt[0] > start else start + dt.timedelta(days=14)
            n = len(b.chapters)
            step = (end - start) / max(1, n)
            dated[b.slug] = [(start + step * k, start + step * (k + 1)) for k in range(n)]
            spans[b.slug] = (start, end)
            b._approx = True
        events = []
        for b in books:
            for n, (c, (s, e)) in enumerate(zip(b.chapters, dated[b.slug]), 1):
                events.append({"book": b.slug, "bookTitle": b.title, "series": b.series, "order": b.order,
                               "chapter": n, "title": c.heading, "page": b.chapter_page(n),
                               "start": s.isoformat(timespec="minutes"), "end": e.isoformat(timespec="minutes"),
                               "approx": bool(getattr(b, "_approx", False))})
        backstory = []
        for g, ent in self.entities.items():
            for ev in ent.record.get("timeline") or []:
                if not isinstance(ev, dict):
                    continue
                text = TAG_RE.sub(r"\3", str(ev.get("event") or ""))
                m = re.match(r"\s*(2[0-3]\d\d)\s*[—\-:]\s*(.+)", text) or (
                    re.match(r"\s*(2[0-3]\d\d)", str(ev.get("date") or "")) and re.match(r"\s*(2[0-3]\d\d)()", str(ev.get("date"))))
                if m:
                    backstory.append({"year": int(m.group(1)), "entity": ent.title, "text": (m.group(2) or text).strip()[:240],
                                      "books": sorted(ent.books)})
        backstory.sort(key=lambda x: (x["year"], x["entity"]))
        return {"books": [{"slug": b.slug, "title": b.title, "series": b.series, "order": b.order,
                           "approx": bool(getattr(b, "_approx", False)), "start": spans[b.slug][0].isoformat(timespec="minutes"),
                           "end": spans[b.slug][1].isoformat(timespec="minutes")} for b in books],
                "events": events, "backstory": backstory}

    def timeline_page(self, data: dict) -> str:
        e = self.e
        dated = sum(1 for b in data["books"] if not b["approx"])
        lines = ["__NOTOC__ __NOEDITSECTION__", "{{DISPLAYTITLE:Timeline}}",
                 f"Everything in the {e(self.u)} books in the order it happens in the world, oldest at the top: every "
                 f"chapter of every book, and the backstory the records date. {dated} of {len(data['books'])} books carry "
                 "Prose's in-world dates; the rest sit where their story order puts them (marked ''approximate''). "
                 "Pick the books to show from the list; events of books you haven't marked read stay hidden as spoilers.",
                 "", '<div id="prose-timeline" class="prose-timeline">Loading the timeline&hellip;</div>']
        return "\n".join(lines) + "\n"

    # ---- the shared graph ----------------------------------------------------------------------------------------
    def graph(self, relationships: list[tuple[str, str, str, str]]) -> dict:
        nodes, edges = [], []
        for g, ent in self.entities.items():
            nodes.append({"id": g, "kind": "entity", "title": ent.title, "repo": ent.repo, "canon": bool(ent.books)})
            for book, n in ent.books.items():
                edges.append({"from": g, "to": f"book:{book}", "kind": "appears_in", "weight": n})
        for b in self.shelf:
            nodes.append({"id": f"book:{b.slug}", "kind": "book", "title": b.title, "series": b.series, "order": b.order,
                          "store": b.store_url or None})
            for n, c in enumerate(b.chapters, 1):
                cid = f"chapter:{b.slug}/{n}"
                nodes.append({"id": cid, "kind": "chapter", "title": c.heading, "book": f"book:{b.slug}", "beats": len(c.beats)})
                counts = collections.Counter(norm(g) for repo, g, _ in TAG_RE.findall("\n".join(c.paras))
                                             if repo != "none" and norm(g) in self.entities)
                edges += [{"from": g, "to": cid, "kind": "named_in", "weight": k} for g, k in counts.items()]
            for r in self.plants.get(b.slug) or []:
                pn, qn = self.chapter_of(b, r.get("PlantBeatId")), self.chapter_of(b, r.get("PayoffBeatId"))
                if pn and qn:
                    edges.append({"from": f"chapter:{b.slug}/{pn}", "to": f"chapter:{b.slug}/{qn}", "kind": "plant_payoff",
                                  "category": r.get("Category"), "plant": r.get("PlantDescription"),
                                  "payoff": r.get("PayoffDescription")})
        edges += [{"from": a, "to": t, "kind": kind, "type": typ} for a, t, kind, typ in relationships]
        for c in self.contradictions:
            ent = self.resolver.by_name(c["entity"])
            edges.append({"from": ent.guid if ent else c["entity"], "to": None, "kind": "contradiction",
                          "predicate": c["predicate"], "claims": c["claims"]})
        return {"universe": self.u, "nodes": nodes, "edges": edges,
                "counts": dict(collections.Counter(x["kind"] for x in nodes)) | {"edges": dict(collections.Counter(x["kind"] for x in edges))}}
