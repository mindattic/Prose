#!/usr/bin/env python3
"""Plan tree -> tickets JSON, with the rules that make a plan "ready to be coded".

    python3 tools/planning/tickets.py docs/planning/read-along-plan.md            # write the .json beside it
    python3 tools/planning/tickets.py docs/planning/read-along-plan.md --check    # validate only

The plan is Markdown with one heading per node:

    ## RA-E2 · Title            epic
    ### RA-F2.1 · Title         feature
    #### RA-S2.1.1 · Title      story      (an *Acceptance:* line follows)
    ##### RA-T2.1.1.1 · Title   task       (- **Field:** value bullets follow)
    ##### RA-D1 · Title         decision

Rules (--check exits 1 on any):
  * ids are unique, and a child's id extends its parent's (RA-T2.1.1.3 sits under RA-S2.1.1);
  * siblings ascend: each child's last number is one more than the previous sibling's;
  * every open task has Est (XS|S|M|L), Depends, Files, and Done when; every open story has an Acceptance;
  * a task depends only on ids that exist and come EARLIER in the plan (ascending build order), or on a decision;
  * a story with no tasks must say it is written out when its decision is ruled.
"""
import json
import re
import sys
from pathlib import Path

HEAD = re.compile(r"^(#{2,5})\s+(RA-[EFSTD][\d.]+)\s+·\s+(.*?)\s*$")
FIELD = re.compile(r"^- \*\*(.+?):\*\*\s*(.*)$")
LEVEL = {"E": "epic", "F": "feature", "S": "story", "T": "task", "D": "decision"}
ID = re.compile(r"^RA-([EFSTD])([\d.]+)$")


def parse(text):
    nodes, cur, field = [], None, None
    for raw in text.splitlines():
        line = raw.rstrip()
        m = HEAD.match(line)
        if m:
            kind = LEVEL[m.group(2)[3]]
            title = re.sub(r"\s+—\s+DONE$", "", m.group(3))
            cur = {"id": m.group(2), "level": kind, "title": title, "status": "done" if m.group(3).endswith("DONE") else "open",
                   "fields": {}, "order": len(nodes)}
            nodes.append(cur)
            field = None
            continue
        if line.startswith("## ") or line.startswith("---"):
            cur, field = None, None
            continue
        if cur is None:
            continue
        acc = re.match(r"^\*Acceptance:\*\s*(.*)$", line)
        if acc:
            cur["fields"]["Acceptance"] = acc.group(1)
            field = "Acceptance"
            continue
        fm = FIELD.match(line)
        if fm:
            field = fm.group(1)
            cur["fields"][field] = fm.group(2)
            continue
        if field and line.startswith("  ") and line.strip():
            cur["fields"][field] += "\n" + line.strip()
    # A task marked "- **Status:** done", or anything under a DONE ancestor, is done.
    by_id = {n["id"]: n for n in nodes}
    for n in nodes:
        if n["fields"].get("Status", "").lower() == "done":
            n["status"] = "done"
        p = parent_id(n["id"])
        while p is not None and n["status"] != "done":
            if by_id.get(p, {}).get("status") == "done":
                n["status"] = "done"
            p = parent_id(p)
    return nodes


def parent_id(nid):
    k, nums = ID.match(nid).groups()
    parts = nums.split(".")
    if k in ("E", "D"):
        return None
    up = {"F": "E", "S": "F", "T": "S"}[k]
    return f"RA-{up}{'.'.join(parts[:-1])}"


def check(nodes):
    errs = []
    by_id = {}
    for n in nodes:
        if n["id"] in by_id:
            errs.append(f"duplicate id {n['id']}")
        by_id[n["id"]] = n
    # parentage and ascending siblings
    last_child = {}
    for n in nodes:
        p = parent_id(n["id"])
        if p is not None and p not in by_id:
            errs.append(f"{n['id']}: parent {p} missing")
        k, nums = ID.match(n["id"]).groups()
        key = (k, p) if k != "D" else ("D", None)
        num = int(nums.split(".")[-1])
        prev = last_child.get(key, 0)
        if num != prev + 1:
            errs.append(f"{n['id']}: expected sibling number {prev + 1}")
        last_child[key] = num
        if p is not None and by_id.get(p, {}).get("order", -1) > n["order"]:
            errs.append(f"{n['id']}: appears before its parent")
    # fields
    for n in nodes:
        if n["status"] == "done":
            continue
        f = n["fields"]
        if n["level"] == "task":
            for req in ("Est", "Depends", "Files", "Done when"):
                if req not in f and not (req == "Depends" and "Status" in f):
                    errs.append(f"{n['id']}: missing {req}")
            if f.get("Est") not in ("XS", "S", "M", "L"):
                errs.append(f"{n['id']}: Est must be XS|S|M|L, got {f.get('Est')!r}")
            deps = [d.strip() for d in re.split(r"[,;]", f.get("Depends", "")) if d.strip() and d.strip() != "—"]
            for d in deps:
                if d not in by_id:
                    errs.append(f"{n['id']}: depends on unknown {d}")
                elif by_id[d]["level"] != "decision" and by_id[d]["order"] > n["order"]:
                    errs.append(f"{n['id']}: depends on later {d} (build order must ascend)")
            n["depends"] = deps
        if n["level"] == "story":
            if "Acceptance" not in f:
                errs.append(f"{n['id']}: story without Acceptance")
            tasks = [m for m in nodes if parent_id(m["id"]) == n["id"]]
            if not tasks and "written out when" not in f.get("Acceptance", ""):
                errs.append(f"{n['id']}: story has no tasks and is not decision-gated")
        if n["level"] == "decision":
            for req in ("Question", "Options", "Record"):
                if req not in f:
                    errs.append(f"{n['id']}: decision missing {req}")
    return errs


def to_json(nodes):
    out = []
    for n in nodes:
        item = {"id": n["id"], "level": n["level"], "title": n["title"], "status": n["status"],
                "parent": parent_id(n["id"])}
        f = n["fields"]
        if "Est" in f: item["estimate"] = f["Est"]
        if n.get("depends"): item["depends"] = n["depends"]
        for src, dst in (("Files", "files"), ("Do", "do"), ("Test", "test"), ("Done when", "doneWhen"),
                         ("Acceptance", "acceptance"), ("Question", "question"), ("Options", "options"), ("Record", "record")):
            if src in f: item[dst] = f[src]
        out.append(item)
    return out


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2
    plan = Path(argv[1])
    nodes = parse(plan.read_text(encoding="utf-8"))
    errs = check(nodes)
    counts = {}
    for n in nodes:
        counts[n["level"]] = counts.get(n["level"], 0) + 1
    open_tasks = [n for n in nodes if n["level"] == "task" and n["status"] == "open"]
    est = {"XS": 0.5, "S": 2, "M": 4.5, "L": 12}
    hours = sum(est.get(n["fields"].get("Est", ""), 0) for n in open_tasks)
    print(f"{plan.name}: " + ", ".join(f"{v} {k[:-1] + 'ies' if k.endswith('y') else k + 's'}" for k, v in counts.items()) + f"; {len(open_tasks)} open tasks (~{hours:.0f} h)")
    if errs:
        print("\n".join("  ✗ " + e for e in errs))
        return 1
    print("  ✓ ids, hierarchy, ascending order, dependencies and required fields are valid")
    if "--check" not in argv:
        target = plan.with_name(plan.stem.replace("-plan", "") + "-tickets.json")
        doc = {"source": plan.as_posix(), "generatedBy": "tools/planning/tickets.py", "tickets": to_json(nodes)}
        target.write_text(json.dumps(doc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        print(f"  wrote {target}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
