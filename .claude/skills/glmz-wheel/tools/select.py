"""Which books need a turn in the next cycle (change-driven Gauss-Seidel).

A book is re-read only when, since its own last turn, another book's applied docket changed a beat
that tags an entity this book also tags. Entities tagged in 15+ books (Arcturus, the Gray Zone...)
are ignored: they change every cycle and say nothing about one book. Only words a row adds or rewrites count, and only when they name the entity; punctuation-only
rows count for nothing. A book's OWN changes do not
re-select it; they were re-read in place when applied.

usage: python select.py <workdir>   (workdir holds B_<slug>.txt dumps and applied.txt in apply order)
"""
import json, re, sys, os, collections
W = sys.argv[1] if len(sys.argv) > 1 else '.'
os.chdir(W)
BOOKS = [l.split()[0] for l in open('books.txt') if l.strip()] if os.path.exists('books.txt') else None
dump = {}
for f in os.listdir('.'):
    m = re.match(r'B_([a-z0-9]+)\.txt$', f)
    if m: dump[m.group(1)] = open(f, encoding='utf-8-sig').read()
BOOKS = BOOKS or sorted(dump)
beats = {}   # slug -> {beatnumber: set(guids)}
tagged = collections.defaultdict(set)
surfaces = collections.defaultdict(set)   # guid -> the words the books tag it with
for t in dump.values():
    for g, w in re.findall(r'<entity repo="[^"]+" guid="([0-9a-f-]{36})">([^<]+)</entity>', t): surfaces[g].add(w)
for s, t in dump.items():
    parts = re.split(r'(?m)^--- \[\d+\] #(\d+) .*---$', t)
    beats[s] = {}
    for i in range(1, len(parts), 2):
        g = set(re.findall(r'guid="([0-9a-f-]{36})"', parts[i + 1]))
        beats[s][parts[i]] = g
        for e in g: tagged[e].add(s)
ubiquitous = {e for e, bs in tagged.items() if len(bs) >= 15}
order = [l.strip() for l in open('applied.txt') if l.strip()]   # docket file names, in apply order
def slug_of(f): return re.match(r'gs_c\w+?_([a-z0-9]+)', f).group(1)
last_turn = {}
for i, f in enumerate(order): last_turn[slug_of(f)] = i
need = collections.defaultdict(set)
for i, f in enumerate(order):
    src = slug_of(f)
    rows = json.load(open(f, encoding='utf-8-sig'))
    changed = set()
    for r in rows:
        old, new = r['old'], r['new']
        delta = new.replace(old, '', 1) if old in new else new   # the words this row adds or rewrites
        if re.sub(r'[\s—\-,.;:]', '', delta) == re.sub(r'[\s—\-,.;:]', '', old if old not in new else ''):
            continue                                             # punctuation-only fix: nothing about the world moved
        for e in beats.get(src, {}).get(str(r['beat']), set()):
            if any(w in delta for w in surfaces[e]): changed.add(e)
        for e, ws in surfaces.items():                           # a borrowed name dropped into the row
            if e not in changed and any(len(w) > 3 and w in delta for w in ws): changed.add(e)
    for e in changed - ubiquitous:
        for b in tagged[e]:
            if b != src and last_turn.get(b, -1) < i:   # changed after b's last turn
                need[b].add(e)
def words(t): return len(re.sub(r'<[^>]+>', '', t).split())
sel = [b for b in BOOKS if need.get(b)]
total_full = total_read = 0
out = {}
print(f'{len(sel)} of {len(BOOKS)} books need a turn:')
for b in sel:
    t = dump[b]
    parts = re.split(r'(?m)^(--- \[(\d+)\] #(\d+) .*---)$', t)
    hits, w_read = [], 0
    for i in range(1, len(parts), 4):
        pos, body = int(parts[i + 1]), parts[i + 3]
        if need[b] & set(re.findall(r'guid="([0-9a-f-]{36})"', body)):
            hits.append(pos); w_read += words(body)
    full = words(t); total_full += full; total_read += w_read
    names = sorted({sorted(surfaces[e], key=len)[-1] for e in need[b]})
    out[b] = {'positions': hits, 'entities': sorted(need[b])}
    print(f'  {b}: {len(need[b])} changed ({", ".join(names[:6])}); read {len(hits)} beats, {w_read:,} of {full:,} words')
print(f'TOTAL to read: {total_read:,} of {total_full:,} words ({100 * total_read // max(total_full, 1)}%)')
json.dump(out, open('select.json', 'w'), indent=1)
