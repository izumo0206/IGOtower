"""Read saved maps and search A/B/C/F/K/O play. Not a Unity runtime test.

python Tools/verify_value_maps.py [stage ...]
Search limits are reported as UNKNOWN, never as proof of impossibility.
"""
import argparse
import csv
import json
import re
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / 'Assets/Resources'


def field(text, key):
    return re.search(r'^  ' + key + r': (.+)$', text, re.M)[1]


def load(n):
    lines = (RES / f'Stage/map{n}.txt').read_text(encoding='utf-8-sig').splitlines()
    hp, cost, roster = lines[0].split()
    grid = [list(map(int, row.split())) for row in lines[1:]][::-1]
    assert len(grid) == 5 and all(len(r) == 5 for r in grid)
    assert all(v >= 1 for r in grid for v in r)
    enemies = []
    for y, row in enumerate(grid):
        for x, v in enumerate(row):
            if v < 2: continue
            t = (RES / f'EnemyUnit/{v}.asset').read_text(encoding='utf-8-sig')
            enemies.append((x, y, int(field(t, 'attackPower')), int(field(t, 'maxHp'))))
    units = []
    for unit_id in roster.removeprefix('allies=').split(','):
        t = (RES / f'AllyUnit/{unit_id}.asset').read_text(encoding='utf-8-sig')
        assert int(field(t, 'skillTiming')) == 1
        units.append((field(t, 'unitName'), int(field(t, 'cost')), int(field(t, 'attackPower')), int(field(t, 'skillType'))))
    return int(hp), int(cost), enemies, units


def solve(hp, cost, enemies, units, seconds=20):
    start = time.monotonic(); nodes = 0; seen = {}
    pos = [y*5+x for x,y,_,_ in enemies]
    neighbors = [tuple(yy*5+xx for xx,yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1))
                       if 0 <= xx < 5 and 0 <= yy < 5) for x,y,_,_ in enemies]
    masks = [sum(1 << p for p in nn) for nn in neighbors]
    dirs = [(0,-1,'up'),(1,0,'right'),(0,1,'down'),(-1,0,'left')]
    effects = {}
    for p in range(25):
        x,y=p%5,p//5
        for name,price,power,skill in units:
            options=[]
            for dx,dy,d in (dirs if skill in (1,3) else [(0,0,'none')]):
                targets=[]
                for i,(ex,ey,_,_) in enumerate(enemies):
                    hit = ((ex,ey)==(x+dx,y+dy)) if skill==1 else (
                        (abs(ex-x)+abs(ey-y)==1) if skill==2 else (
                        ((dx and ey==y and (ex-x)*dx>0) or (dy and ex==x and (ey-y)*dy>0)) if skill==3 else False))
                    if hit:targets.append(i)
                options.append((d,tuple(targets)))
            effects[p,name]=options

    def visit(hps, health, occupied, remaining):
        nonlocal nodes
        nodes+=1
        if nodes % 512 == 0 and time.monotonic()-start > seconds: raise TimeoutError
        if all(h<=0 for h in hps): return []
        if remaining<=0:return None
        key=(hps,occupied,remaining)
        if seen.get(key,-1)>=health:return None
        seen[key]=health
        live={p for p,h in zip(pos,hps) if h>0}
        actions=[]; dedup=set()
        for p in range(25):
            if p in live or occupied & (1<<p):continue
            if sum(p in nn and h>0 for nn,h in zip(neighbors,hps))>=4:continue
            placed=occupied|(1<<p)
            for name,price,power,skill in units:
                if price>remaining:continue
                for direction,targets in effects[p,name]:
                    after=list(hps)
                    for i in targets:
                        if after[i]>0:after[i]=max(0,after[i]-power)
                    # RegisterUnit checks only enemies adjacent to this placement.
                    for i,h in enumerate(after):
                        if h>0 and p in neighbors[i] and placed & masks[i] == masks[i]:after[i]=0
                    after=tuple(after)
                    damage=sum(e[2] for e,h in zip(enemies,after) if h>0)
                    nxt=health+(power if skill==4 else 0)-damage
                    if nxt<=0:continue
                    state=(after,nxt,placed,remaining-price)
                    if state in dedup:continue
                    dedup.add(state)
                    killed=sum(e[2] for e,a,b in zip(enemies,hps,after) if a>0 and b==0)
                    progress=sum(a-b for a,b in zip(hps,after))
                    score=killed*100+progress*8+nxt-price*2
                    actions.append((score,state,(chr(97+p%5)+str(p//5+1),name,direction,nxt)))
        actions.sort(key=lambda a:a[0],reverse=True)
        for _,state,step in actions:
            rest=visit(*state)
            if rest is not None:return [step]+rest
        return None
    try:
        result=visit(tuple(e[3] for e in enemies),hp,0,cost)
        return dict(status='SOLVED' if result is not None else 'NO_SOLUTION',nodes=nodes,steps=result)
    except TimeoutError:return dict(status='UNKNOWN',nodes=nodes,steps=None)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('stages',nargs='*',type=int)
    parser.add_argument('--csv',type=Path,help='Optional original CSV for independent source comparison')
    args=parser.parse_args()
    manifest=json.loads((ROOT/'Docs/駒価値マップ_データ.json').read_text(encoding='utf-8'))
    requested=set(args.stages) if args.stages else set(range(15,24))
    unit_o=(RES/'AllyUnit/015.asset').read_text(encoding='utf-8-sig')
    assert field(unit_o,'unitName')=='O'
    assert tuple(int(field(unit_o,k)) for k in ('cost','maxHp','attackPower','skillTiming','skillType'))==(2,2,3,1,1)
    source=None
    if args.csv:
        with args.csv.open(encoding='utf-8-sig',newline='') as stream:source=list(csv.reader(stream))
    results=[]
    for entry in manifest:
        n=entry['stage']
        if n not in requested:continue
        hp,cost,enemies,units=load(n)
        assert (hp,cost,[list(e) for e in enemies],[u[0] for u in units]) == (
            entry['hp'],entry['cost'],entry['enemies'],entry['allies'])
        if source:
            i=entry['csv_row']-1
            assert (hp,cost)==(int(source[i+1][8]),int(source[i+3][8]))
            assert [u[0] for u in units]==source[i+2][2].split(',')
            expected=[]
            for y in range(5):
                for x,cell in enumerate(source[i+y][3:8]):
                    if cell!='b':expected.append((x,y,*map(int,cell.split('/'))))
            assert enemies==expected, f'CSV mismatch: map{n}'
        result=dict(stage=n,**solve(hp,cost,enemies,units))
        print(json.dumps(result),flush=True);results.append(result)
    (ROOT/'Logs').mkdir(exist_ok=True)
    (ROOT/'Logs/value-map-solutions.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
    if any(r['status']!='SOLVED' for r in results):sys.exit(1)


if __name__=='__main__':main()
