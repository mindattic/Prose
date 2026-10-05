# print only the words each row adds or changes, for hand review
import json,sys,difflib
s,c=sys.argv[1],sys.argv[2]
for r in json.load(open(f'gs_c{c}_{s}.json',encoding='utf-8-sig')):
    o,n=r['old'],r['new']
    if o in n: print(f"#{r['beat']} + {n.replace(o,'',1).strip()}")
    else: print(f"#{r['beat']} ~ {o!r}\n      -> {n!r}")
