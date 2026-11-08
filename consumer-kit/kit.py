#!/usr/bin/env python3
"""The Prose consumer kit (RFC 0017, docs/CONDUIT.md section 6): one copy, shipped from the Prose repo, run by
path from every game that keeps its story in Prose.

    python <Prose>/consumer-kit/kit.py <command> [options]        (or kit.ps1 on Windows)

Commands:
    validate                    the universe file and every pending text, against the schemas and the contract
    map                         run the game's mapper (config "mapper") to rewrite the universe file
    push [--map] [--prune] [--merge] [--no-check] [--batch-size N]
                                validate, import in batches as one session (one Outbox notice), then export back,
                                diff and leak-check
    diff [<ours> <theirs>] [--allow-stale]
                                semantic diff of two interchange files (none: the universe file against the Hub),
                                Prose-authored entities (export origin "prose") set aside
    leak [<ours> <snapshot> [--export <file>]]
                                every node and edge of a Hub /snapshot?scope=all must be the file's (none: live),
                                Prose-authored entities named by the export set aside
    pull [--kind K] [--since V] [--status S] [--all]
                                approved/published texts newer than applied.jsonl into pending/
    apply [--dry-run]           run the game's applier over pending/, record applied texts, clear them
    status                      the universe on the Hub, texts by kind and status, pending, last applied
    outbox [--peek]             drain /api/outbox/<consumer> into outbox.jsonl

Global options: --config <path to prose.config.json> (default: Data/Prose/prose.config.json found walking up from
the current directory), --hub <url>.

Standard library only (Python 3.9+). `validate` also uses the jsonschema package when it is installed.
Exit codes: 0 ok, 1 a check failed, 2 usage or configuration error.
"""

from __future__ import annotations

import argparse
import collections
import datetime as _dt
import json
import os
import re
import secrets
import shlex
import subprocess
import sys
import time
import unicodedata
import urllib.error
import urllib.parse
import urllib.request

KIT_VERSION = 1
KIT_DIR = os.path.dirname(os.path.abspath(__file__))
PROSE_ROOT = os.path.dirname(KIT_DIR)
SCHEMA_DIR = os.path.join(PROSE_ROOT, "docs", "schemas")
DEFAULT_HUB = "http://127.0.0.1:5900"
DEFAULT_HUB_EXE = r"C:\Apps\MindAttic\Prose\Hub.exe"
CONFIG_RELATIVE = os.path.join("Data", "Prose", "prose.config.json")

STABLE_ID = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
KEY_RE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9_.:/-]*$")
PLACEHOLDER = re.compile(r"^\$[A-Za-z0-9_]+$")
GUID_RE = re.compile(r"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")
TEXT_TYPE = "prose-text"
KINDS = ("conversation", "crawl", "description", "quest", "book", "bark", "glossary")
TEXT_STATUSES = ("draft", "approved", "published", "retired")


class KitError(Exception):
    """A usage or configuration problem (exit 2)."""


# ------------------------------------------------------------------------------------------------ config


class Config:
    def __init__(self, path: str, data: dict):
        self.path = os.path.abspath(path)
        self.dir = os.path.dirname(self.path)
        self.data = data
        need = [k for k in ("consumer", "universe") if not data.get(k)]
        if need:
            raise KitError(f"{path}: missing {', '.join(need)}")
        if int(data.get("kit", 1)) > KIT_VERSION:
            raise KitError(f"{path} asks for kit contract {data['kit']}, this kit is {KIT_VERSION}: update the Prose checkout")
        self.consumer = data["consumer"]
        self.universe = data["universe"]
        if not STABLE_ID.match(self.universe):
            raise KitError(f"universe '{self.universe}' is not a stable slug")
        self.display_name = data.get("displayName", self.universe)
        self.root = self._p(data.get("root", os.path.join("..", "..")))
        self.universe_file = self._p(data.get("universeFile", f"{self.universe}.universe.json"))
        self.pending = self._p(data.get("pending", "pending"))
        self.applied = self._p(data.get("applied", "applied.jsonl"))
        self.outbox_log = self._p(data.get("outboxLog", "outbox.jsonl"))
        self.scratch = self._p(data.get("scratch", ".kit"))
        self.mapper = data.get("mapper")
        self.applier = data.get("applier")
        self.kinds = list(data.get("kinds") or KINDS)
        bad = [k for k in self.kinds if k not in KINDS]
        if bad:
            raise KitError(f"unknown kinds in config: {bad} (known: {', '.join(KINDS)})")
        self.tags = data.get("tags")
        self.batch_size = int(data.get("batchSize", 400))
        self.prune = bool(data.get("prune", True))
        self.hub = data.get("hub", DEFAULT_HUB)

    def _p(self, rel: str) -> str:
        return os.path.normpath(os.path.join(self.dir, rel))


def find_config(explicit: str | None) -> Config:
    path = explicit or os.environ.get("PROSE_KIT_CONFIG")
    if not path:
        d = os.getcwd()
        while True:
            cand = os.path.join(d, CONFIG_RELATIVE)
            if os.path.isfile(cand):
                path = cand
                break
            parent = os.path.dirname(d)
            if parent == d:
                raise KitError(f"no {CONFIG_RELATIVE} here or above {os.getcwd()} (pass --config)")
            d = parent
    if not os.path.isfile(path):
        raise KitError(f"config not found: {path}")
    return Config(path, load(path))


# ------------------------------------------------------------------------------------------------ json helpers


def load(path: str):
    with open(path, encoding="utf-8-sig") as f:
        return json.load(f)


def write_json(path: str, doc) -> None:
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=2)
        f.write("\n")


def read_jsonl(path: str) -> list:
    if not os.path.isfile(path):
        return []
    out = []
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if line:
                try:
                    out.append(json.loads(line))
                except json.JSONDecodeError:
                    pass
    return out


def append_jsonl(path: str, rows: list) -> None:
    if not rows:
        return
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with open(path, "a", encoding="utf-8", newline="\n") as f:
        for r in rows:
            f.write(json.dumps(r, ensure_ascii=False, sort_keys=True) + "\n")


def now_iso() -> str:
    return _dt.datetime.now(_dt.timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


# ------------------------------------------------------------------------------------------------ the Hub


class Hub:
    def __init__(self, base: str):
        self.base = base.rstrip("/")
        self._key = None

    @property
    def key(self) -> str:
        if self._key is None:
            self._key = os.environ.get("PROSE_HUB_KEY") or ""
            if not self._key:
                settings = os.path.join(os.environ.get("LOCALAPPDATA", ""), "MindAttic", "Prose", "Settings.json")
                if os.path.isfile(settings):
                    self._key = load(settings).get("HubApiKey") or ""
        return self._key

    def _request(self, method: str, path: str, params=None, body=None, timeout=600, keyed=False):
        url = self.base + path
        if params:
            q = {k: v for k, v in params.items() if v is not None}
            if q:
                url += "?" + urllib.parse.urlencode(q)
        data = None
        headers = {}
        if body is not None:
            data = body if isinstance(body, bytes) else json.dumps(body).encode("utf-8")
            headers["Content-Type"] = "application/json; charset=utf-8"
        if keyed:
            if not self.key:
                raise KitError("no Hub API key: set PROSE_HUB_KEY or start the Hub once (it writes HubApiKey to "
                               "%LOCALAPPDATA%\\MindAttic\\Prose\\Settings.json)")
            headers["X-Prose-Key"] = self.key
        req = urllib.request.Request(url, data=data, method=method, headers=headers)
        try:
            with urllib.request.urlopen(req, timeout=timeout) as resp:
                raw = resp.read()
        except urllib.error.HTTPError as e:
            raw = e.read()
            try:
                detail = json.loads(raw)
            except Exception:
                detail = raw.decode("utf-8", "replace")[:500]
            raise HubError(e.code, detail) from None
        if not raw:
            return None
        if path.endswith("/snapshot") or raw[:1] in (b"{", b"["):
            return json.loads(raw)
        return raw.decode("utf-8", "replace")

    def get(self, path, params=None, keyed=False, timeout=600):
        return self._request("GET", path, params, None, timeout, keyed)

    def post(self, path, body, params=None, timeout=3600):
        return self._request("POST", path, params, body, timeout, keyed=True)

    def up(self) -> bool:
        try:
            self.get("/api/health", timeout=3)
            return True
        except Exception:
            return False

    def ensure_up(self) -> None:
        if self.up():
            return
        exe = os.environ.get("PROSE_HUB_EXE", DEFAULT_HUB_EXE)
        if not os.path.isfile(exe):
            raise KitError(f"the Prose Hub is not running at {self.base} and {exe} is missing: deploy Prose "
                           "(run.ps1 -Apps Hub -Start Hub in the Prose repo)")
        print(f"Prose Hub down: starting {exe}")
        flags = getattr(subprocess, "CREATE_NO_WINDOW", 0) | getattr(subprocess, "DETACHED_PROCESS", 0)
        subprocess.Popen([exe], cwd=os.path.dirname(exe), creationflags=flags, close_fds=True)
        deadline = time.time() + 90
        while not self.up():
            if time.time() > deadline:
                raise KitError(f"the Prose Hub did not come up at {self.base} within 90 s")
            time.sleep(2)
        print("Prose Hub up")


class HubError(Exception):
    def __init__(self, status, detail):
        super().__init__(f"HTTP {status}: {detail}")
        self.status = status
        self.detail = detail


# ------------------------------------------------------------------------------------------------ validation


def validate_universe(doc, require_universe: str | None = None) -> list:
    errors = []
    u = doc.get("universe") if isinstance(doc, dict) else None
    if not isinstance(u, dict) or not u.get("id") or not u.get("name"):
        errors.append("universe.id and universe.name are required")
    elif not STABLE_ID.match(str(u["id"])):
        errors.append(f"universe.id '{u['id']}' is not a stable slug")
    elif require_universe and u["id"] != require_universe:
        errors.append(f"universe.id '{u['id']}' is not the config's universe '{require_universe}'")
    if isinstance(doc, dict) and "source" in doc and not isinstance(doc["source"], dict):
        errors.append("source must be an object")
    ents = doc.get("entities") if isinstance(doc, dict) else None
    if not isinstance(ents, list):
        return errors + ["entities must be an array"]
    ids = collections.Counter()
    for i, e in enumerate(ents):
        if not isinstance(e, dict):
            errors.append(f"entities[{i}] is not an object")
            continue
        for k in ("id", "type", "name", "summary"):
            if not isinstance(e.get(k), str):
                errors.append(f"entities[{i}].{k} missing or not a string")
        eid = e.get("id")
        if isinstance(eid, str):
            ids[eid] += 1
            if not STABLE_ID.match(eid):
                errors.append(f"entities[{i}].id '{eid}' is not a stable slug (Prose would rename it)")
        if isinstance(e.get("type"), str) and e["type"].strip().lower() == TEXT_TYPE:
            errors.append(f"entities[{i}] uses the reserved type '{TEXT_TYPE}'")
        if "details" in e and not isinstance(e["details"], dict):
            errors.append(f"entities[{i}].details must be an object")
        for r in e.get("relations", []) or []:
            if not isinstance(r, dict) or not isinstance(r.get("to"), str) or not isinstance(r.get("kind"), str):
                errors.append(f"entities[{i}] has a relation without to/kind")
        if "tags" in e and not isinstance(e["tags"], list):
            errors.append(f"entities[{i}].tags must be an array")
    for i, n in ids.items():
        if n > 1:
            errors.append(f"duplicate id '{i}' ({n} times)")
    known = set(ids)
    dangling = [(e.get("id"), r.get("to")) for e in ents if isinstance(e, dict)
                for r in (e.get("relations") or []) if isinstance(r, dict) and r.get("to") not in known]
    if dangling:
        errors.append(f"{len(dangling)} relations point at ids not in the file (Prose would invent stubs), e.g. {dangling[:3]}")
    return errors


def _str(o, name) -> bool:
    return isinstance(o, dict) and isinstance(o.get(name), str) and o[name].strip() != ""


def _lines(p, name) -> list:
    arr = p.get(name)
    if not isinstance(arr, list) or not arr:
        return [f"payload.{name} must be a non-empty array of strings"]
    return [f"payload.{name}[{i}] is empty or not a string" for i, l in enumerate(arr) if not isinstance(l, str) or not l.strip()]


CONTEXT_FIELDS = ("place", "chapter", "order", "trigger", "after")


def validate_context(ctx) -> list:
    """Errors for the optional prose-text/1 context block (RFC 0018): where and when a text appears in the game."""
    if not isinstance(ctx, dict):
        return ["context must be an object"]
    errs = [f"context.{k} is not a context field ({', '.join(CONTEXT_FIELDS)})" for k in ctx if k not in CONTEXT_FIELDS]
    place = ctx.get("place")
    if place is not None and not (isinstance(place, str) and STABLE_ID.match(place)):
        errs.append("context.place must be an entity id")
    for k in ("chapter", "trigger"):
        v = ctx.get(k)
        if v is not None and not (isinstance(v, str) and v.strip()):
            errs.append(f"context.{k} must be a non-empty string")
    order = ctx.get("order")
    if order is not None and (isinstance(order, bool) or not isinstance(order, (int, float))):
        errs.append("context.order must be a number")
    after = ctx.get("after")
    if after is not None:
        if not isinstance(after, list):
            errs.append("context.after must be an array of text ids")
        else:
            errs += [f"context.after item '{a}' is not a text id" for a in after
                     if not isinstance(a, str) or not STABLE_ID.match(a)]
    return errs


def validate_payload(kind: str, p, speakers: list) -> list:
    """The same checks Prose makes on draft (ProseTextService.ValidatePayload), so a pending file is known good."""
    if not isinstance(p, dict):
        return ["payload must be an object"]
    errs = []
    if kind == "conversation":
        if not _str(p, "start"):
            errs.append("conversation needs payload.start")
        nodes = p.get("nodes")
        if not isinstance(nodes, list) or not nodes:
            return errs + ["conversation needs payload.nodes (a non-empty array)"]
        ids, nexts = set(), []
        for i, n in enumerate(nodes):
            if not _str(n, "id"):
                errs.append(f"nodes[{i}] needs an id")
                continue
            nid = n["id"]
            if nid in ids:
                errs.append(f"node id '{nid}' is used twice")
            ids.add(nid)
            if not _str(n, "speaker"):
                errs.append(f"node '{nid}' needs a speaker")
            elif n["speaker"] not in speakers:
                errs.append(f"node '{nid}' speaker '{n['speaker']}' is not one of the text's speakers")
            if not _str(n, "line"):
                errs.append(f"node '{nid}' needs a non-empty line")
            if n.get("next") is not None:
                if _str(n, "next"):
                    nexts.append((nid, n["next"]))
                else:
                    errs.append(f"node '{nid}' next must be a non-empty string")
            for c, ch in enumerate(n.get("choices") or []):
                if not _str(ch, "text"):
                    errs.append(f"node '{nid}' choice {c} needs text")
                if not _str(ch, "next"):
                    errs.append(f"node '{nid}' choice {c} needs next")
                else:
                    nexts.append((nid, ch["next"]))
        if _str(p, "start") and p["start"] not in ids:
            errs.append(f"start '{p['start']}' is not a node")
        for frm, nx in nexts:
            if not nx.startswith("$") and nx not in ids:
                errs.append(f"node '{frm}' goes to '{nx}', which is not a node")
    elif kind == "crawl":
        if not _str(p, "title"):
            errs.append("crawl needs payload.title")
        errs += _lines(p, "paragraphs")
    elif kind == "description":
        if not _str(p, "short") and not _str(p, "long"):
            errs.append("description needs payload.short or payload.long")
    elif kind == "quest":
        if not _str(p, "title"):
            errs.append("quest needs payload.title")
        if not _str(p, "brief"):
            errs.append("quest needs payload.brief")
    elif kind == "book":
        if not _str(p, "title"):
            errs.append("book needs payload.title")
        errs += _lines(p, "pages")
    elif kind == "bark":
        if not _str(p, "speaker"):
            errs.append("bark needs payload.speaker")
        elif speakers and p["speaker"] not in speakers:
            errs.append(f"bark speaker '{p['speaker']}' is not one of the text's speakers")
        errs += _lines(p, "lines")
    elif kind == "glossary":
        if not _str(p, "term"):
            errs.append("glossary needs payload.term")
        if not _str(p, "definition"):
            errs.append("glossary needs payload.definition")
    return errs


def choice_tags(doc) -> set:
    tags = set()
    if doc.get("kind") == "conversation" and isinstance(doc.get("payload"), dict):
        for n in doc["payload"].get("nodes") or []:
            for ch in (n.get("choices") or []) if isinstance(n, dict) else []:
                for t in (ch.get("tags") or []) if isinstance(ch, dict) else []:
                    tags.add(t)
    return tags


def validate_text(doc, universe: str | None = None, kinds=None, tags=None) -> tuple:
    """(errors, warnings) for one prose-text/1 document."""
    errs, warns = [], []
    if not isinstance(doc, dict):
        return ["not an object"], []
    if doc.get("schema") != "prose-text/1":
        errs.append("schema must be 'prose-text/1'")
    if not isinstance(doc.get("id"), str) or not STABLE_ID.match(doc["id"]):
        errs.append(f"id '{doc.get('id')}' is not a stable slug")
    if doc.get("guid") is not None and not (isinstance(doc["guid"], str) and GUID_RE.match(doc["guid"])):
        errs.append(f"guid '{doc.get('guid')}' is not a GUID")
    if universe and doc.get("universe") != universe:
        errs.append(f"universe '{doc.get('universe')}' is not '{universe}'")
    kind = doc.get("kind")
    if kind not in KINDS:
        errs.append(f"kind '{kind}' is not one of {', '.join(KINDS)}")
    elif kinds and kind not in kinds:
        warns.append(f"kind '{kind}' is not in the config's kinds")
    if not isinstance(doc.get("key"), str) or not KEY_RE.match(doc["key"]):
        errs.append(f"key '{doc.get('key')}' is not a valid key")
    if not isinstance(doc.get("version"), int) or doc["version"] < 1:
        errs.append("version must be a positive integer")
    if doc.get("status") not in TEXT_STATUSES:
        errs.append(f"status '{doc.get('status')}' is not one of {', '.join(TEXT_STATUSES)}")
    about = doc.get("about") or []
    speakers = doc.get("speakers") or []
    errs += [f"about '{a}' is not an entity id" for a in about if not isinstance(a, str) or not STABLE_ID.match(a)]
    errs += [f"speaker '{s}' is neither an entity id nor a $placeholder" for s in speakers
             if not isinstance(s, str) or not (STABLE_ID.match(s) or PLACEHOLDER.match(s))]
    if kind in KINDS and doc.get("status") != "retired":
        errs += validate_payload(kind, doc.get("payload"), speakers)
    if doc.get("context") is not None:
        errs += validate_context(doc["context"])
    if tags is not None:
        unknown = sorted(choice_tags(doc) - set(tags))
        if unknown:
            warns.append(f"choice tags {unknown} are not in the config's tags")
    return errs, warns


def jsonschema_errors(doc, schema_file: str) -> list:
    try:
        import jsonschema  # type: ignore
    except ImportError:
        return []
    path = os.path.join(SCHEMA_DIR, schema_file)
    if not os.path.isfile(path):
        return []
    v = jsonschema.Draft202012Validator(load(path))
    return [f"schema: {'/'.join(str(p) for p in e.path)}: {e.message}"
            for e in sorted(v.iter_errors(doc), key=lambda e: list(e.path))[:20]]


# ------------------------------------------------------------------------------------------------ diff and leak


def stats_of(doc) -> dict:
    ents = doc["entities"]
    types = collections.Counter(e["type"] for e in ents)
    kinds = collections.Counter(r["kind"] for e in ents for r in e.get("relations", []) or [])
    return {"entities": len(ents), "edges": sum(kinds.values()),
            "entitiesByType": dict(sorted(types.items())), "edgesByKind": dict(sorted(kinds.items()))}


def canon(e) -> dict:
    """An entity as Prose stores it (its interchange record): absent details as {}, relations as a sorted set."""
    return {"id": e.get("id", ""), "type": e.get("type", ""), "name": e.get("name", ""), "summary": e.get("summary", ""),
            "details": e.get("details") or {},
            "relations": sorted((r.get("to", ""), r.get("kind", "")) for r in e.get("relations", []) or []),
            "tags": e.get("tags", []) or []}


def prose_authored(doc) -> list:
    """The entities of an export Prose authored itself (origin "prose", PRS-T923): not the game's, so the round trip
    sets them aside. A file without origins (an older Hub, or the game's own file) has none."""
    return [e for e in doc.get("entities") or [] if isinstance(e, dict) and e.get("origin") == "prose"]


def without_prose_authored(doc) -> tuple:
    """(doc without its Prose-authored entities and every relation to one, how many were set aside)."""
    authored = prose_authored(doc)
    if not authored:
        return doc, 0
    gone = {e.get("id") for e in authored}
    ents = [dict(e, relations=[r for r in e.get("relations", []) or [] if r.get("to") not in gone])
            for e in doc["entities"] if e.get("origin") != "prose"]
    return dict(doc, entities=ents), len(authored)


def diff(a_path: str, b_path: str, allow_stale=False, out=print) -> int:
    a, b = load(a_path), load(b_path)
    # Prose-authored canon (origin "prose") and its relations to the game's entities are Prose's, not the game's:
    # compared, they would read as entities "only in theirs" and fail every push (GridGame's first push: 33).
    b, set_aside = without_prose_authored(b)
    problems = []
    ua, ub = a.get("universe", {}), b.get("universe", {})
    for k in sorted(set(ua) | set(ub)):
        if ua.get(k) != ub.get(k):
            problems.append(f"universe.{k}: {json.dumps(ua.get(k))[:80]} != {json.dumps(ub.get(k))[:80]}")
    ea = {e["id"]: canon(e) for e in a["entities"]}
    eb = {e["id"]: canon(e) for e in b["entities"]}
    only_a, only_b = sorted(set(ea) - set(eb)), sorted(set(eb) - set(ea))
    changed = [(i, [k for k in ea[i] if ea[i][k] != eb[i][k]]) for i in sorted(set(ea) & set(eb)) if ea[i] != eb[i]]
    if allow_stale and only_b:
        b = dict(b, entities=[e for e in b["entities"] if e["id"] in ea])
    sa, sb = stats_of(a), stats_of(b)
    out(f"ours:   {sa['entities']} entities, {sa['edges']} edges  ({a_path})")
    out(f"theirs: {sb['entities']} entities, {sb['edges']} edges  ({b_path})"
        + (f"; {set_aside} Prose-authored entities set aside" if set_aside else ""))
    for t in sorted(set(sa["entitiesByType"]) | set(sb["entitiesByType"])):
        x, y = sa["entitiesByType"].get(t, 0), sb["entitiesByType"].get(t, 0)
        if x != y:
            problems.append(f"type {t}: {x} != {y}")
    for k in sorted(set(sa["edgesByKind"]) | set(sb["edgesByKind"])):
        x, y = sa["edgesByKind"].get(k, 0), sb["edgesByKind"].get(k, 0)
        if x != y:
            problems.append(f"edge kind {k}: {x} != {y}")
    if only_a:
        problems.append(f"{len(only_a)} entities only in ours, e.g. {only_a[:5]}")
    if only_b:
        msg = f"{len(only_b)} entities only in theirs (push with --prune to retire them), e.g. {only_b[:5]}"
        if allow_stale:
            out("  stale: " + msg)
        else:
            problems.append(msg)
    if changed:
        problems.append(f"{len(changed)} entities differ, e.g. {changed[:5]}")
    if problems:
        out("DIFFERS")
        for p in problems:
            out("  " + p)
        return 1
    out("IDENTICAL: 0 entity diffs, 0 relation diffs, 0 universe diffs")
    return 0


def name_slug(name: str) -> str:
    """UniverseGraphService.Slugify of an entity's NAME: the graph node id of a name only one entity has (PRS-T916)."""
    folded = "".join(c for c in unicodedata.normalize("NFD", name or "") if unicodedata.category(c) != "Mn")
    for x, y in (("ß", "ss"), ("æ", "ae"), ("Æ", "ae"), ("ø", "o"), ("Ø", "o"), ("œ", "oe"), ("Œ", "oe"), ("ł", "l"), ("Ł", "l")):
        folded = folded.replace(x, y)
    return re.sub(r"[^a-z0-9]+", "-", folded.lower().strip()).strip("-")


def leak(file_path: str, snap_path: str, out=print, export_path: str | None = None) -> int:
    """The Hub's graph view of the universe against the file. Texts (nodeType prose-text) and retired entities
    (status archived) are Prose's own and are ignored, with every edge that touches them. With the universe's export
    (export_path), so are the entities Prose authored itself (origin "prose", matched to nodes by entityId = guid,
    PRS-T923) and their edges to the game's entities; any other node, from another universe or nowhere, still leaks."""
    doc, snap = load(file_path), load(snap_path)
    authored = {str(e.get("guid")).lower() for e in prose_authored(load(export_path)) if e.get("guid")} if export_path else set()
    ents = doc["entities"]
    by_id = {e["id"]: e for e in ents}
    names = {name_slug(e["name"]) for e in ents} | set(by_id)
    types = {e["type"] for e in ents} | {"place", "stub"}
    all_nodes = snap.get("nodes") or []
    # Compare by the node's NAME slug, not its id (PRS-T921): a Hub keying nodes per entity (PRS-T916) gives every
    # same-named entity but the primary the id "<name-slug>~<entity id>", and the file's relations are by name too.
    key = {n.get("id"): (name_slug(n["name"]) if n.get("name") else n.get("id")) for n in all_nodes}

    def retired(n):
        return str(n.get("status", "")).lower() in ("archived", "retired")

    live_keys = {key[n.get("id")] for n in all_nodes if not retired(n) and n.get("nodeType") != TEXT_TYPE}
    live_names = {name_slug(e["name"]) for e in ents}
    # A retired node is Prose's own and ignored, with its edges, unless it is the only node of a name a live file
    # entity has: an older Hub merged same-named entities into one node that reads archived after a prune while it
    # carries the live entity's edges (SpaceGod: 161 false MISSING edges from 55 live/retired same-name pairs).
    ignored = {n.get("id") for n in all_nodes
               if n.get("nodeType") == TEXT_TYPE
               or (retired(n) and not (key[n.get("id")] in live_names and key[n.get("id")] not in live_keys))}
    prose_nodes = {n.get("id") for n in all_nodes if str(n.get("entityId") or "").lower() in authored} - ignored
    ignored |= prose_nodes
    nodes = [n for n in all_nodes if n.get("id") not in ignored]
    edges = [x for x in snap.get("edges") or [] if x.get("source") not in ignored and x.get("target") not in ignored]
    foreign = [n.get("id") for n in nodes if str(key[n.get("id")]) not in names and str(n.get("id")) not in names]
    foreign_types = collections.Counter(n.get("nodeType") for n in nodes if n.get("nodeType") not in types)
    ours = {(name_slug(e["name"]), name_slug(by_id[r["to"]]["name"]), r["kind"])
            for e in ents for r in e.get("relations", []) or [] if r.get("to") in by_id}
    theirs = {(key.get(x.get("source"), x.get("source")), key.get(x.get("target"), x.get("target")), x.get("relationType"))
              for x in edges}
    extra, missing = theirs - ours, ours - theirs
    out(f"snapshot: {len(nodes)} nodes, {len(edges)} edges ({len(ignored) - len(prose_nodes)} text/retired nodes ignored"
        + (f", {len(prose_nodes)} Prose-authored set aside" if prose_nodes else "") + "); "
        f"file: {len(ents)} entities, {len(ours)} distinct relations by name")
    bad = False
    if foreign or foreign_types:
        out(f"LEAK    {len(foreign)} nodes are not entities of the file, e.g. {foreign[:8]}; foreign types {dict(foreign_types)}")
        bad = True
    if extra:
        out(f"LEAK    {len(extra)} edges Prose holds that the file does not, e.g. {sorted(extra)[:5]}")
        bad = True
    if missing:
        out(f"MISSING {len(missing)} of the file's relations are not edges in Prose, e.g. {sorted(missing)[:5]}")
        bad = True
    if not bad:
        out("no leak: every graph node is one of the file's entities and the graph's edges are exactly the file's relations")
    return 1 if bad else 0


# ------------------------------------------------------------------------------------------------ applied log


def applied_index(path: str) -> dict:
    """id -> the newest applied entry."""
    idx = {}
    for row in read_jsonl(path):
        if isinstance(row, dict) and row.get("id"):
            idx[row["id"]] = row
    return idx


def needs_pull(doc, applied: dict) -> bool:
    last = applied.get(doc["id"])
    if last is None:
        return doc.get("status") != "retired"
    if doc.get("status") == "retired":
        return last.get("status") != "retired"
    return doc["version"] > int(last.get("version", 0)) or doc.get("hash") != last.get("hash")


# ------------------------------------------------------------------------------------------------ commands


def run_shell(command: str, cwd: str, env_extra: dict | None = None, capture=False):
    env = dict(os.environ)
    env.update(env_extra or {})
    args = command if os.name == "nt" else shlex.split(command)
    return subprocess.run(args, cwd=cwd, env=env, shell=(os.name == "nt"), text=True, encoding="utf-8",
                          capture_output=capture)


def cmd_validate(cfg: Config, args) -> int:
    failed = False
    if os.path.isfile(cfg.universe_file):
        doc = load(cfg.universe_file)
        errs = validate_universe(doc, cfg.universe) + jsonschema_errors(doc, "universe.schema.json")
        if errs:
            failed = True
            print(f"INVALID {cfg.universe_file}")
            for e in errs[:40]:
                print("  " + e)
        else:
            print(f"valid   {cfg.universe_file}: {len(doc['entities'])} entities, "
                  f"{sum(len(e.get('relations', []) or []) for e in doc['entities'])} relations")
    elif not args.texts_only:
        print(f"no universe file yet at {cfg.universe_file} (run the mapper: kit map)")
    texts = pending_files(cfg)
    bad = 0
    for path in texts:
        doc = load(path)
        errs, warns = validate_text(doc, cfg.universe, cfg.kinds, cfg.tags)
        errs += jsonschema_errors(doc, "prose-text.schema.json")
        for w in warns:
            print(f"warning {os.path.basename(path)}: {w}")
        if errs:
            bad += 1
            print(f"INVALID {os.path.basename(path)}")
            for e in errs[:20]:
                print("  " + e)
    if texts:
        print(f"{len(texts) - bad}/{len(texts)} pending text(s) valid")
    return 1 if failed or bad else 0


def cmd_map(cfg: Config, args) -> int:
    if not cfg.mapper:
        raise KitError("no mapper in the config")
    print(f"map: {cfg.mapper}")
    r = run_shell(cfg.mapper, cfg.root, {"PROSE_KIT_UNIVERSE_FILE": cfg.universe_file, "PROSE_KIT_UNIVERSE": cfg.universe})
    if r.returncode != 0:
        print(f"the mapper failed (exit {r.returncode})")
        return 1
    if not os.path.isfile(cfg.universe_file):
        print(f"the mapper wrote no {cfg.universe_file}")
        return 1
    return 0


def batches_of(doc, size: int) -> list:
    ents = doc["entities"]
    size = max(1, size)
    out = []
    for i in range(0, max(len(ents), 1), size):
        b = {"universe": doc["universe"], "entities": ents[i:i + size]}
        if "source" in doc:
            b["source"] = doc["source"]
        out.append(b)
    return out


def cmd_push(cfg: Config, args, hub: Hub) -> int:
    if args.map and cmd_map(cfg, args) != 0:
        return 1
    if not os.path.isfile(cfg.universe_file):
        raise KitError(f"no universe file at {cfg.universe_file} (run kit map, or push --map)")
    doc = load(cfg.universe_file)
    errs = validate_universe(doc, cfg.universe)
    if errs:
        print(f"INVALID {cfg.universe_file}; not pushed")
        for e in errs[:20]:
            print("  " + e)
        return 1
    hub.ensure_up()
    mode = "merge" if args.merge else ("replace" if (args.prune or cfg.prune) else "merge")
    batches = batches_of(doc, args.batch_size or cfg.batch_size)
    session = f"kit-{time.strftime('%Y%m%d%H%M%S')}-{secrets.token_hex(3)}"
    tot = collections.Counter()
    started = time.time()
    last = {}
    for i, b in enumerate(batches, 1):
        final = i == len(batches)
        params = {"mode": mode, "session": session, "final": "true" if final else "false"}
        body = json.dumps(b, ensure_ascii=False).encode("utf-8")
        for attempt in (1, 2):
            try:
                last = hub.post(f"/api/universes/{cfg.universe}/import", body, params) or {}
                break
            except HubError as e:
                if attempt == 2 or e.status < 500:
                    print(f"Prose refused batch {i}/{len(batches)}: {e}")
                    return 1
                print(f"  batch {i}: {e}; retrying")
                hub.ensure_up()
            except (urllib.error.URLError, ConnectionError, TimeoutError) as e:
                if attempt == 2:
                    print(f"batch {i}/{len(batches)} failed twice: {e}")
                    return 1
                print(f"  batch {i}: {e}; retrying on a restarted Hub")
                hub.ensure_up()
        for k in ("entitiesCreated", "entitiesUpdated", "entitiesUnchanged", "edgesCreated", "stubsCreated", "stubsPromoted"):
            tot[k] += int(last.get(k, 0) or 0)
        print(f"  batch {i}/{len(batches)}: created {last.get('entitiesCreated', 0)}, updated {last.get('entitiesUpdated', 0)} "
              f"({last.get('entitiesUnchanged', 0)} unchanged), edges {last.get('edgesCreated', 0)}  [{time.time() - started:.0f} s]")
    print(f"push    {cfg.universe} ({mode}): created {tot['entitiesCreated']}, updated {tot['entitiesUpdated']} "
          f"({tot['entitiesUnchanged']} unchanged), edges {tot['edgesCreated']}, stubs {tot['stubsCreated'] - tot['stubsPromoted']} left"
          + (f", retired {last.get('entitiesRetired', 0)} ({last.get('pruneBasis')}), relations invalidated "
             f"{last.get('edgesInvalidated', 0)}" if mode == "replace" else "")
          + f" in {len(batches)} batch(es), {time.time() - started:.0f} s")
    if args.no_check:
        return 0
    return round_trip(cfg, hub, allow_stale=(mode == "merge"))


def fetch_export(cfg: Config, hub: Hub) -> str | None:
    """Export the universe from the Hub into the kit's scratch folder; the path, or None when Prose wrote nothing."""
    os.makedirs(cfg.scratch, exist_ok=True)
    back = os.path.join(cfg.scratch, f"{cfg.universe}.prose-export.json")
    if os.path.isfile(back):
        os.remove(back)
    req = {"toolClass": "UniverseInterchangeTools", "method": "ExportUniverseFileImpl", "universe": cfg.universe,
           "scopeExplicit": True, "args": {"slug": cfg.universe, "path": os.path.abspath(back)}}
    hub.post("/api/mcp-invoke", req, timeout=1800)
    if not os.path.isfile(back):
        print(f"Prose wrote no export at {back}")
        return None
    return back


def fetch_snapshot(cfg: Config, hub: Hub) -> str:
    os.makedirs(cfg.scratch, exist_ok=True)
    snap = os.path.join(cfg.scratch, f"{cfg.universe}.prose-snapshot.json")
    write_json(snap, hub.get(f"/api/universes/{cfg.universe}/snapshot", {"scope": "all"}, timeout=600))
    return snap


def round_trip(cfg: Config, hub: Hub, allow_stale: bool) -> int:
    back = fetch_export(cfg, hub)
    if back is None:
        return 1
    failed = diff(cfg.universe_file, back, allow_stale=allow_stale) != 0
    snap = fetch_snapshot(cfg, hub)
    failed = leak(cfg.universe_file, snap, export_path=back) != 0 or failed
    print("round trip " + ("FAILED" if failed else f"clean: the file and Prose's '{cfg.universe}' universe hold the same entities and edges"))
    return 1 if failed else 0


def cmd_live_check(cfg: Config, args, hub: Hub) -> int:
    """`diff` / `leak` with no files: the config's universe file against what the Hub holds now. Changes nothing in
    Prose (an export and a snapshot into the kit's scratch folder)."""
    if not os.path.isfile(cfg.universe_file):
        raise KitError(f"no universe file at {cfg.universe_file} (run kit map)")
    hub.ensure_up()
    back = fetch_export(cfg, hub)
    if back is None:
        return 1
    if args.cmd == "diff":
        return diff(cfg.universe_file, back, allow_stale=args.allow_stale)
    return leak(cfg.universe_file, fetch_snapshot(cfg, hub), export_path=back)


def pending_files(cfg: Config) -> list:
    if not os.path.isdir(cfg.pending):
        return []
    return sorted(os.path.join(cfg.pending, f) for f in os.listdir(cfg.pending) if f.endswith(".json"))


def pending_name(doc) -> str:
    return f"{doc['id']}.v{doc['version']}.json"


def cmd_pull(cfg: Config, args, hub: Hub) -> int:
    hub.ensure_up()
    status = args.status or "pullable"
    docs = hub.get(f"/api/universes/{cfg.universe}/texts", {"kind": args.kind, "status": status, "since": args.since}) or []
    applied = {} if args.all else applied_index(cfg.applied)
    os.makedirs(cfg.pending, exist_ok=True)
    wrote = skipped = refused = 0
    for doc in docs:
        if doc.get("kind") not in cfg.kinds:
            skipped += 1
            continue
        if not needs_pull(doc, applied):
            skipped += 1
            continue
        errs, _ = validate_text(doc, cfg.universe)
        if errs:
            refused += 1
            print(f"refused {doc.get('id')}: {'; '.join(errs[:3])}")
            continue
        for old in pending_files(cfg):
            if os.path.basename(old).startswith(doc["id"] + ".v"):
                os.remove(old)
        write_json(os.path.join(cfg.pending, pending_name(doc)), doc)
        wrote += 1
        print(f"pending {pending_name(doc)}  [{doc['kind']}] {doc['key']} ({doc['status']})")
    print(f"pull    {cfg.universe}: {wrote} to pending, {skipped} already applied or not this game's kinds, {refused} refused")
    return 1 if refused else 0


def cmd_apply(cfg: Config, args) -> int:
    if not cfg.applier:
        raise KitError("no applier in the config")
    files = pending_files(cfg)
    if not files:
        print("nothing pending")
        return 0
    by_key = {}
    for f in files:
        d = load(f)
        by_key[(d.get("id"), d.get("version"))] = (f, d)
    env = {"PROSE_KIT_PENDING": cfg.pending, "PROSE_KIT_UNIVERSE": cfg.universe, "PROSE_KIT_CONSUMER": cfg.consumer,
           "PROSE_KIT_DRY_RUN": "1" if args.dry_run else "0"}
    print(f"apply: {cfg.applier}{' (dry run)' if args.dry_run else ''}")
    r = run_shell(cfg.applier, cfg.root, env, capture=True)
    if r.stderr:
        sys.stderr.write(r.stderr)
    if r.returncode != 0:
        print(f"the applier failed (exit {r.returncode}); nothing recorded")
        return 1
    rows, counts = [], collections.Counter()
    for line in (r.stdout or "").splitlines():
        line = line.strip()
        if not line.startswith("{"):
            if line:
                print("  " + line)
            continue
        try:
            rep = json.loads(line)
        except json.JSONDecodeError:
            print("  " + line)
            continue
        res = rep.get("result")
        counts[res] += 1
        key = (rep.get("id"), rep.get("version"))
        note = f" ({rep['note']})" if rep.get("note") else ""
        if res != "applied":
            print(f"  {res:8} {rep.get('id')} v{rep.get('version')}{note}")
            continue
        if key not in by_key:
            print(f"  applier reported {key} applied, but no such pending file; not recorded")
            continue
        path, doc = by_key[key]
        if rep.get("hash") and doc.get("hash") and rep["hash"] != doc["hash"]:
            print(f"  applier reported {key} with a different hash; not recorded")
            continue
        print(f"  applied  {doc['id']} v{doc['version']}  {doc['key']}{note}")
        if not args.dry_run:
            row = {"id": doc["id"], "version": doc["version"], "hash": doc.get("hash"), "key": doc.get("key"),
                   "kind": doc.get("kind"), "status": doc.get("status"), "appliedAt": now_iso()}
            if doc.get("guid"):
                row["guid"] = doc["guid"]  # PRS-T923: the text's Prose GUID, passed through from the pull
            rows.append(row)
            os.remove(path)
    append_jsonl(cfg.applied, rows)
    print(f"apply   {cfg.universe}: {counts['applied']} applied, {counts['orphan']} orphan, {counts['skipped']} skipped"
          f"{' (dry run: nothing recorded)' if args.dry_run else f'; {len(rows)} recorded in ' + os.path.basename(cfg.applied)}")
    return 0


def cmd_status(cfg: Config, args, hub: Hub) -> int:
    print(f"{cfg.display_name}: consumer '{cfg.consumer}', universe '{cfg.universe}', kit {KIT_VERSION}, config {cfg.path}")
    hub.ensure_up()
    universes = hub.get("/api/universes") or []
    if not any(u.get("slug") == cfg.universe for u in universes):
        print(f"  universe '{cfg.universe}' does not exist on the Hub yet (kit push creates it)")
    else:
        try:
            st = hub.get(f"/api/universes/{cfg.universe}/stats")
            print(f"  graph: {json.dumps(st)[:300]}")
        except HubError as e:
            print(f"  graph stats unavailable: {e}")
        docs = hub.get(f"/api/universes/{cfg.universe}/texts", {"status": "all"}) or []
        c = collections.Counter((d.get("kind"), d.get("status")) for d in docs)
        print(f"  texts: {len(docs)}" + "".join(f"\n    {k:12} {s:10} {n}" for (k, s), n in sorted(c.items())))
    applied = read_jsonl(cfg.applied)
    print(f"  pending: {len(pending_files(cfg))}  applied: {len(applied_index(cfg.applied))} text(s)"
          + (f", last {applied[-1].get('id')} v{applied[-1].get('version')} at {applied[-1].get('appliedAt')}" if applied else ""))
    return 0


def cmd_outbox(cfg: Config, args, hub: Hub) -> int:
    hub.ensure_up()
    events = hub.get(f"/api/outbox/{cfg.consumer}", {"peek": "true" if args.peek else None}, keyed=True) or []
    if isinstance(events, dict):
        events = [events]
    print(f"{len(events)} outbox event(s) for '{cfg.consumer}'{' (peek)' if args.peek else ''}")
    for e in events:
        print(f"- [{e.get('kind')}] {e.get('summary')}")
    if not args.peek:
        append_jsonl(cfg.outbox_log, events)
    return 0


# ------------------------------------------------------------------------------------------------ main


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(prog="kit", description="The Prose consumer kit (docs/CONDUIT.md).")
    ap.add_argument("--config")
    ap.add_argument("--hub")
    sub = ap.add_subparsers(dest="cmd")
    v = sub.add_parser("validate")
    v.add_argument("--texts-only", action="store_true")
    sub.add_parser("map")
    p = sub.add_parser("push")
    p.add_argument("--map", action="store_true")
    p.add_argument("--prune", action="store_true", help="replace mode: retire entities the file no longer has (the config's prune default)")
    p.add_argument("--merge", action="store_true", help="merge mode: never retire, whatever the config says")
    p.add_argument("--no-check", action="store_true")
    p.add_argument("--batch-size", type=int)
    d = sub.add_parser("diff", help="with no files: the config's universe file against a fresh Hub export")
    d.add_argument("ours", nargs="?")
    d.add_argument("theirs", nargs="?")
    d.add_argument("--allow-stale", action="store_true")
    lk = sub.add_parser("leak", help="with no files: the config's universe file against a fresh Hub snapshot")
    lk.add_argument("ours", nargs="?")
    lk.add_argument("snapshot", nargs="?")
    lk.add_argument("--export", help="the universe's export, so Prose-authored entities (origin prose) are set aside")
    pl = sub.add_parser("pull")
    pl.add_argument("--kind")
    pl.add_argument("--since")
    pl.add_argument("--status", help="pullable (default), approved, published, retired")
    pl.add_argument("--all", action="store_true", help="ignore applied.jsonl")
    a = sub.add_parser("apply")
    a.add_argument("--dry-run", action="store_true")
    sub.add_parser("status")
    o = sub.add_parser("outbox")
    o.add_argument("--peek", action="store_true")
    sub.add_parser("version")
    args = ap.parse_args(argv)
    try:
        if args.cmd in (None,):
            ap.print_help()
            return 2
        if args.cmd == "version":
            print(f"prose consumer kit {KIT_VERSION} ({KIT_DIR})")
            return 0
        if args.cmd == "diff" and args.theirs:
            return diff(args.ours, args.theirs, args.allow_stale)
        if args.cmd == "leak" and args.snapshot:
            return leak(args.ours, args.snapshot, export_path=args.export)
        if args.cmd in ("diff", "leak") and args.ours:
            raise KitError(f"{args.cmd} takes two files, or none (live: the config's universe file against the Hub)")
        cfg = find_config(args.config)
        hub = Hub(args.hub or cfg.hub)
        if args.cmd in ("diff", "leak"):
            return cmd_live_check(cfg, args, hub)
        if args.cmd == "validate":
            return cmd_validate(cfg, args)
        if args.cmd == "map":
            return cmd_map(cfg, args)
        if args.cmd == "push":
            return cmd_push(cfg, args, hub)
        if args.cmd == "pull":
            return cmd_pull(cfg, args, hub)
        if args.cmd == "apply":
            return cmd_apply(cfg, args)
        if args.cmd == "status":
            return cmd_status(cfg, args, hub)
        if args.cmd == "outbox":
            return cmd_outbox(cfg, args, hub)
    except KitError as e:
        print(f"kit: {e}", file=sys.stderr)
        return 2
    except HubError as e:
        print(f"kit: the Hub said {e}", file=sys.stderr)
        return 1
    return 2


if __name__ == "__main__":
    sys.exit(main())
