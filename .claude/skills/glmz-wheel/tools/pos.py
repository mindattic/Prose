import json,re,sys,os
SP=os.getcwd()  # run from the work dir
s,f=sys.argv[1],sys.argv[2]
t=open(f'{SP}/B_{s}.txt',encoding='utf-8-sig').read()
m={b:int(p) for p,b in re.findall(r'(?m)^--- \[(\d+)\] #(\d+) ',t)}
rows=json.load(open(f,encoding='utf-8-sig'))
print(' '.join(str(x) for x in sorted({m[str(r["beat"])] for r in rows})))
