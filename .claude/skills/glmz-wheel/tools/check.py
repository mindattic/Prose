import json, re, sys, os
SP = os.getcwd()  # run from the work dir
BAN = [
 (r'—|--', 'em dash or --'),
 (r'(?i)\b(coins?|cash|banknotes?|paper money|wallets?|smart ?phones?|cell ?phones?|phones?|tablets?|slates?|datapads?|monitors?|terminals?|keyboards?|handsets?|burners?)\b', 'device/cash word'),
 (r'(?i)\bscreens?\b', 'screen'),
 (r'(?i)\b(police|cops?|courts?|courtrooms?|judges?|lawyers?|attorneys?|trials?|sheriffs?|detectives?)\b', 'police/court word'),
 (r'\bHR\b', 'HR abbreviation'),
 (r'(?i)\bgrey', 'grey spelling'),
 (r'(?i)\barcholog', 'archology spelling'),
 (r'(?i)\b(turret cats?|null crows?)\b', 'retired stray name'),
 (r'(?i)\b(new york|california|florida|manhattan|los angeles|miami)\b', 'Blur place'),
 (r'\bNarrows\b', 'Narrows'),
 (r'(?i)\bup the well\b', 'up the well'),
 (r'(?i)(?<!Atmospheric )\bProcessors\b', 'Processors without Atmospheric'),
]
WARN = [(r'(?i)\bsigns?\b', 'sign (must be in-air/hyperreality)'), (r'(?i)\bscreen', 'screen')]

def beats(path):
    t = open(path, encoding='utf-8-sig').read()
    parts = re.split(r'(?m)^--- \[(\d+)\] #(\d+) .*---$', t)
    d = {}
    for i in range(1, len(parts), 3):
        d[parts[i+1]] = (int(parts[i]), parts[i+2])
    return d

def main(slug, cyc):
    B = beats(os.path.join(SP, f'B_{slug}.txt'))
    rows = json.load(open(os.path.join(SP, f'gs_c{cyc}_{slug}.json'), encoding='utf-8-sig'))
    err = 0
    seen = set()
    for r in rows:
        b, old, new = str(r['beat']), r['old'], r['new']
        tag = f"#{b}"
        if b not in B: print('ERROR', tag, 'no such beat'); err += 1; continue
        pos, txt = B[b]
        c = txt.count(old)
        if c != 1: print('ERROR', tag, f'old occurs {c}x:', old[:70]); err += 1
        if '<' in old or '>' in old: print('ERROR', tag, 'old has markup'); err += 1
        # old must not sit inside a tag's text span
        i = txt.find(old)
        if i >= 0:
            pre = txt[:i]
            if pre.count('<entity') > pre.count('</entity>'): print('ERROR', tag, 'old starts inside an entity tag'); err += 1
        if (b, old) in seen: print('ERROR', tag, 'duplicate row'); err += 1
        seen.add((b, old))
        if r.get('count', 1) != 1: print('ERROR', tag, 'count must be 1'); err += 1
        added = new.replace(old, '', 1) if old in new else new
        for pat, why in BAN:
            if re.search(pat, added) and not re.search(pat, old): print('ERROR', tag, why, '::', added.strip()[:110]); err += 1
        for pat, why in WARN:
            if re.search(pat, added) and not re.search(pat, old): print('WARN ', tag, why, '::', added.strip()[:110])
        if old not in new: print('WARN ', tag, 'old not kept verbatim in new')
    print(f'{slug}: {len(rows)} rows, {err} errors')

main(sys.argv[1], sys.argv[2])
