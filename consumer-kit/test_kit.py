"""Offline tests for the Prose consumer kit (python -m unittest consumer-kit/test_kit.py). A fake Hub on a local port
stands in for Prose, so nothing here touches the real Hub or its database."""

import contextlib
import io
import json
import os
import shutil
import sys
import tempfile
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit  # noqa: E402


def universe(*ents, uid="game"):
    return {"universe": {"id": uid, "name": "Game"}, "entities": list(ents)}


def ent(i, t="character", rel=(), name=None):
    return {"id": i, "type": t, "name": name or i.title(), "summary": "s", "relations": [{"to": r, "kind": "knows"} for r in rel], "tags": []}


def text(i="description-a", version=1, status="approved", kind="description", payload=None, h="sha256:" + "0" * 64):
    return {"schema": "prose-text/1", "id": i, "universe": "game", "kind": kind, "key": "a", "about": [], "speakers": [],
            "version": version, "status": status, "hash": h, "payload": payload or {"short": "A."}}


class FakeHub:
    def __init__(self):
        self.requests = []
        self.texts = []
        self.outbox = [{"id": "1", "kind": "texts.published", "summary": "2 text(s) published"}]
        hub = self

        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *a):
                pass

            def _send(self, obj, code=200):
                raw = json.dumps(obj).encode()
                self.send_response(code)
                self.send_header("Content-Type", "application/json")
                self.send_header("Content-Length", str(len(raw)))
                self.end_headers()
                self.wfile.write(raw)

            def do_GET(self):
                u = urlparse(self.path)
                q = {k: v[0] for k, v in parse_qs(u.query).items()}
                hub.requests.append(("GET", u.path, q, None, self.headers.get("X-Prose-Key")))
                if u.path == "/api/health":
                    return self._send({"ok": True})
                if u.path.endswith("/texts"):
                    return self._send([t for t in hub.texts if not q.get("kind") or t["kind"] == q["kind"]])
                if u.path.startswith("/api/outbox/"):
                    return self._send(hub.outbox)
                return self._send({"error": "not_found"}, 404)

            def do_POST(self):
                u = urlparse(self.path)
                q = {k: v[0] for k, v in parse_qs(u.query).items()}
                body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
                hub.requests.append(("POST", u.path, q, body, self.headers.get("X-Prose-Key")))
                return self._send({"entitiesCreated": len(body.get("entities", [])), "entitiesUpdated": 0, "edgesCreated": 0,
                                   "entitiesRetired": 1 if q.get("final") == "true" else 0, "pruneBasis": "manifest"})

        self.server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        self.url = f"http://127.0.0.1:{self.server.server_address[1]}"
        threading.Thread(target=self.server.serve_forever, daemon=True).start()

    def close(self):
        self.server.shutdown()
        self.server.server_close()


class KitTests(unittest.TestCase):
    def setUp(self):
        self.root = tempfile.mkdtemp(prefix="prose-kit-")
        self.data = os.path.join(self.root, "Data", "Prose")
        os.makedirs(self.data)
        self.hub = FakeHub()
        os.environ["PROSE_HUB_KEY"] = "test-key"
        applier = os.path.join(self.root, "apply.py")
        with open(applier, "w", encoding="utf-8") as f:
            f.write(
                "import json, os, sys\n"
                "d = os.environ['PROSE_KIT_PENDING']\n"
                "for n in sorted(os.listdir(d)):\n"
                "    t = json.load(open(os.path.join(d, n), encoding='utf-8'))\n"
                "    res = 'orphan' if t['key'] == 'unused' else 'applied'\n"
                "    print(json.dumps({'id': t['id'], 'version': t['version'], 'hash': t['hash'], 'result': res}))\n")
        self.cfg_path = os.path.join(self.data, "prose.config.json")
        kit.write_json(self.cfg_path, {"kit": 1, "consumer": "game", "universe": "game", "root": "../..", "hub": self.hub.url,
                                       "applier": f'"{sys.executable}" apply.py', "kinds": ["description", "conversation"],
                                       "tags": ["right-answer"], "batchSize": 2})
        self.cfg = kit.Config(self.cfg_path, kit.load(self.cfg_path))

    def tearDown(self):
        self.hub.close()
        shutil.rmtree(self.root, ignore_errors=True)

    def run_kit(self, *argv):
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            code = kit.main(["--config", self.cfg_path, *argv])
        return code, out.getvalue()

    # -- validation

    def test_universe_contract_checks(self):
        self.assertEqual(kit.validate_universe(universe(ent("a", rel=["b"]), ent("b")), "game"), [])
        errs = kit.validate_universe(universe(ent("A_bad"), ent("b", rel=["zzz"]), ent("b"), ent("t", t="prose-text")), "game")
        joined = "\n".join(errs)
        self.assertIn("not a stable slug", joined)
        self.assertIn("duplicate id 'b'", joined)
        self.assertIn("reserved type", joined)
        self.assertIn("point at ids not in the file", joined)
        self.assertIn("not the config's universe", "\n".join(kit.validate_universe(universe(uid="other"), "game")))

    def test_text_checks_match_prose(self):
        convo = text("conversation-x", kind="conversation", payload={
            "start": "a", "nodes": [{"id": "a", "speaker": "$hero", "line": "Hi", "choices": [
                {"text": "Yes", "next": "b", "tags": ["right-answer"]}, {"text": "No", "next": "$outcome", "tags": ["odd"]}]},
                {"id": "b", "speaker": "$hero", "line": "Good."}]})
        convo["speakers"] = ["$hero"]
        errs, warns = kit.validate_text(convo, "game", ["conversation"], ["right-answer"])
        self.assertEqual(errs, [])
        self.assertTrue(any("odd" in w for w in warns))
        convo["payload"]["nodes"][1]["id"] = "c"
        errs, _ = kit.validate_text(convo, "game")
        self.assertTrue(any("'b', which is not a node" in e for e in errs))
        errs, _ = kit.validate_text(text(kind="crawl", payload={"title": "T", "paragraphs": [""]}), "game")
        self.assertTrue(any("paragraphs[0]" in e for e in errs))

    # -- diff and leak

    def test_diff_and_leak(self):
        a = os.path.join(self.root, "a.json")
        b = os.path.join(self.root, "b.json")
        kit.write_json(a, universe(ent("x", rel=["y"]), ent("y")))
        kit.write_json(b, universe(ent("x", rel=["y"]), ent("y"), ent("stale")))
        sink = []
        self.assertEqual(kit.diff(a, a, out=sink.append), 0)
        self.assertEqual(kit.diff(a, b, out=sink.append), 1)
        self.assertEqual(kit.diff(a, b, allow_stale=True, out=sink.append), 0)

        snap = os.path.join(self.root, "snap.json")
        kit.write_json(snap, {"nodes": [{"id": "x", "nodeType": "character"}, {"id": "y", "nodeType": "character"},
                                        {"id": "a-wounded-ronin", "nodeType": "prose-text", "status": "canon"},
                                        {"id": "gone", "nodeType": "character", "status": "archived"}],
                              "edges": [{"source": "x", "target": "y", "relationType": "knows"},
                                        {"source": "a-wounded-ronin", "target": "x", "relationType": "about"},
                                        {"source": "gone", "target": "x", "relationType": "knows"}]})
        self.assertEqual(kit.leak(a, snap, out=sink.append), 0, "texts and retired rows are Prose's own")
        kit.write_json(snap, {"nodes": [{"id": "x", "nodeType": "character"}, {"id": "intruder", "nodeType": "weapon"}], "edges": []})
        self.assertEqual(kit.leak(a, snap, out=sink.append), 1)

    # -- push

    def test_push_is_one_replace_session_with_final_on_the_last_batch(self):
        kit.write_json(self.cfg.universe_file, universe(ent("a"), ent("b"), ent("c"), uid="game"))
        code, out = self.run_kit("push", "--no-check")
        self.assertEqual(code, 0, out)
        posts = [r for r in self.hub.requests if r[0] == "POST"]
        self.assertEqual(len(posts), 2, "3 entities in batches of 2")
        self.assertEqual({p[2]["session"] for p in posts}.__len__(), 1, "one session id")
        self.assertEqual([p[2]["final"] for p in posts], ["false", "true"])
        self.assertTrue(all(p[2]["mode"] == "replace" for p in posts), "the config prunes by default")
        self.assertTrue(all(p[4] == "test-key" for p in posts))
        self.assertIn("retired 1", out)

    def test_push_refuses_an_invalid_file(self):
        kit.write_json(self.cfg.universe_file, universe(ent("a", rel=["missing"])))
        code, out = self.run_kit("push", "--no-check")
        self.assertEqual(code, 1)
        self.assertFalse([r for r in self.hub.requests if r[0] == "POST"])

    # -- pull and apply

    def test_pull_then_apply_records_the_applied_log_and_skips_what_is_applied(self):
        self.hub.texts = [text("description-a", 2, "published"), text("description-b", 1, "approved"),
                          text("glossary-z", 1, "approved", kind="glossary", payload={"term": "z", "definition": "d"})]
        orphan = text("description-o", 1, "approved")
        orphan["key"] = "unused"
        self.hub.texts.append(orphan)
        kit.append_jsonl(self.cfg.applied, [{"id": "description-b", "version": 1, "hash": "sha256:" + "0" * 64, "status": "approved"}])

        code, out = self.run_kit("pull")
        self.assertEqual(code, 0, out)
        names = sorted(os.listdir(self.cfg.pending))
        self.assertEqual(names, ["description-a.v2.json", "description-o.v1.json"], "b is applied; glossary is not this game's kind")

        code, out = self.run_kit("apply")
        self.assertEqual(code, 0, out)
        self.assertEqual(os.listdir(self.cfg.pending), ["description-o.v1.json"], "an orphan stays pending")
        applied = kit.applied_index(self.cfg.applied)
        self.assertEqual(applied["description-a"]["version"], 2)
        self.assertNotIn("description-o", applied)

        code, out = self.run_kit("pull")
        self.assertNotIn("description-a", " ".join(os.listdir(self.cfg.pending)))

    def test_apply_dry_run_records_nothing(self):
        self.hub.texts = [text("description-a")]
        self.run_kit("pull")
        code, out = self.run_kit("apply", "--dry-run")
        self.assertEqual(code, 0, out)
        self.assertEqual(kit.read_jsonl(self.cfg.applied), [])
        self.assertEqual(len(os.listdir(self.cfg.pending)), 1)

    def test_retired_texts_are_pulled_only_when_applied_before(self):
        applied = {"description-a": {"id": "description-a", "version": 1, "hash": "h", "status": "approved"}}
        self.assertTrue(kit.needs_pull(text("description-a", 1, "retired"), applied))
        self.assertFalse(kit.needs_pull(text("description-q", 1, "retired"), applied))
        applied["description-a"]["status"] = "retired"
        self.assertFalse(kit.needs_pull(text("description-a", 1, "retired"), applied))

    # -- outbox and config

    def test_outbox_drains_into_the_journal(self):
        code, out = self.run_kit("outbox")
        self.assertEqual(code, 0, out)
        self.assertIn("texts.published", out)
        self.assertEqual(len(kit.read_jsonl(self.cfg.outbox_log)), 1)
        self.run_kit("outbox", "--peek")
        self.assertEqual(len(kit.read_jsonl(self.cfg.outbox_log)), 1, "peek journals nothing")

    def test_config_is_found_by_walking_up_and_rejects_a_newer_contract(self):
        sub = os.path.join(self.root, "a", "b")
        os.makedirs(sub)
        cwd = os.getcwd()
        try:
            os.chdir(sub)
            os.environ.pop("PROSE_KIT_CONFIG", None)
            self.assertEqual(kit.find_config(None).path, os.path.abspath(self.cfg_path))
        finally:
            os.chdir(cwd)
        with self.assertRaises(kit.KitError):
            kit.Config(self.cfg_path, {"kit": kit.KIT_VERSION + 1, "consumer": "g", "universe": "g"})
        with self.assertRaises(kit.KitError):
            kit.Config(self.cfg_path, {"consumer": "g", "universe": "g", "kinds": ["poem"]})


if __name__ == "__main__":
    unittest.main()
