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
        # beat id -> (book, chapter number)
        self.where = {bid: (b, n) for b in shelf for n, c in enumerate(b.chapters, 1) for bid in c.beats}

    # ---- helpers -------------------------------------------------------------------------------------------------
    def chapter_link(self, b, n: int | None) -> str:
        if not n:
            return "&mdash;"
        return f"[[{b.chapter_page(n)}|{n}. {self.e(b.chapters[n - 1].heading)}]]"

    def chapter_of(self, beat_id) -> int | None:
        hit = self.where.get(norm(beat_id))
        return hit[1] if hit else None

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
        pages[f"Project:Continuity"] = self.continuity_page()
        return pages

    def plants_page(self, b, rows) -> str:
        e = self.e
        rows = sorted(rows, key=lambda r: (self.chapter_of(r.get("PlantBeatId")) or 999, r.get("SortKey") or 0))
        lines = [f"{{{{DISPLAYTITLE:Plants and payoffs in <i>{e(b.title)}</i>}}}}", "__NOTOC__",
                 f"'''Spoilers.''' Every plant ''[[{b.page}|{e(b.title)}]]'' sets up and where it pays off, as Prose has "
                 f"them registered: {len(rows)} in all. A plant is planted so a re-reader finds it waiting; the payoff "
                 "is what they get.", "",
                 '{| class="wikitable prose-plants"', "! Planted !! What is seeded !! Paid off !! What the reader gets !! Kind"]
        unplaced = 0
        for r in rows:
            pn, qn = self.chapter_of(r.get("PlantBeatId")), self.chapter_of(r.get("PayoffBeatId"))
            unplaced += not pn or not qn
            lines += ["|-", f"| {self.chapter_link(b, pn)} || {e(r.get('PlantDescription') or '')} || "
                            f"{self.chapter_link(b, qn)} || {e(r.get('PayoffDescription') or '')} || {e(r.get('Category') or '')}"]
        lines.append("|}")
        if unplaced:
            lines += ["", f"''{unplaced} of these point at a beat the current text no longer has (moved or cut): "
                          "Prose's registry needs them re-pointed.''"]
        lines += ["", f"[[Category:Plants and payoffs]]"]
        return "\n".join(lines) + "\n"

    def pacing_page(self, b, plants) -> str:
        e = self.e
        set_in, paid_in = collections.Counter(), collections.Counter()
        for r in plants:
            if (n := self.chapter_of(r.get("PlantBeatId"))):
                set_in[n] += 1
            if (n := self.chapter_of(r.get("PayoffBeatId"))):
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
                pn, qn = self.chapter_of(r.get("PlantBeatId")), self.chapter_of(r.get("PayoffBeatId"))
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
