# template trap: 4-grams in a candidate docket's added text that already appear in text added to OTHER books this wheel
import json, re, sys, os
# run from the work dir
s, c = sys.argv[1], sys.argv[2]
STOP = set('the a an of and to in on at it was were is that with for from by as his her their its this had have has not but or he she they them him into out up down over under one'.split())
def grams(t):
    w = re.findall(r"[a-z']+", t.lower())
    return {' '.join(w[i:i+4]) for i in range(len(w)-3) if sum(x not in STOP for x in w[i:i+4]) >= 2}
def added(r):
    o, n = r['old'], r['new']
    return n.replace(o, '', 1) if o in n else n
applied = [l.strip() for l in open('applied.txt') if l.strip()] if os.path.exists('applied.txt') else []
seen = {}
for f in applied:
    b = f.split('_', 2)[2][:-5]
    if b == s:
        continue
    for r in json.load(open(f, encoding='utf-8-sig')):
        for g in grams(added(r)):
            seen.setdefault(g, b)
hits = 0
for r in json.load(open(f'gs_c{c}_{s}.json', encoding='utf-8-sig')):
    for g in sorted(grams(added(r))):
        if g in seen:
            print(f'TEMPLATE #{r["beat"]}: "{g}" already used in {seen[g]}')
            hits += 1
print(f'{hits} template hits')
