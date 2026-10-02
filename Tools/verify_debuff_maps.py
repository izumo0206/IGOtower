"""CSV comparison and A/C/G/H search for map16-18. Not a Unity runtime test."""
import csv
import json
import sys
import time
from pathlib import Path

from verify_value_maps import load


def solve(hp, cost, enemies, units, seconds=20):
    assert all(u[3] in (1, 4, 5, 6) for u in units)
    positions = [y * 5 + x for x, y, _, _ in enemies]
    neighbors = [tuple(yy * 5 + xx for xx, yy in ((x-1,y),(x+1,y),(x,y-1),(x,y+1))
                       if 0 <= xx < 5 and 0 <= yy < 5) for x,y,_,_ in enemies]
    masks = [sum(1 << p for p in nn) for nn in neighbors]
    directions = ((0,-1,'up'),(1,0,'right'),(0,1,'down'),(-1,0,'left'))
    started = time.monotonic()
    seen = {}
    nodes = 0

    def visit(hps, attacks, health, occupied, remaining):
        nonlocal nodes
        nodes += 1
        if nodes % 512 == 0 and time.monotonic() - started > seconds:
            raise TimeoutError
        if not any(h > 0 for h in hps):
            return []
        if remaining <= 0:
            return None
        key = (hps, attacks, occupied, remaining)
        if seen.get(key, -1) >= health:
            return None
        seen[key] = health
        candidates = []
        outcomes = set()
        for p in range(25):
            if occupied & (1 << p) or any(h > 0 and e == p for e,h in zip(positions,hps)):
                continue
            if sum(h > 0 and p in nn for h,nn in zip(hps,neighbors)) == 4:
                continue
            for name, price, power, skill in units:
                if price > remaining:
                    continue
                for dx,dy,direction in (directions if skill in (1,5) else ((0,0,'none'),)):
                    after = list(hps)
                    reduced = list(attacks)
                    for i,(x,y,_,_) in enumerate(enemies):
                        if after[i] <= 0:
                            continue
                        hit = (x,y) == (p%5+dx,p//5+dy) if skill in (1,5) else p in neighbors[i]
                        if hit and skill == 1:
                            after[i] = max(0,after[i]-power)
                        if hit and skill in (5,6):
                            reduced[i] = max(0,reduced[i]-power)
                    placed = occupied | (1 << p)
                    for i,h in enumerate(after):
                        if h > 0 and p in neighbors[i] and placed & masks[i] == masks[i]:
                            after[i] = 0
                    damage = sum(a for a,h in zip(reduced,after) if h > 0)
                    nxt = health + (power if skill == 4 else 0) - damage
                    if nxt <= 0:
                        continue
                    state = (tuple(after),tuple(reduced),nxt,placed,remaining-price)
                    if state in outcomes:
                        continue
                    outcomes.add(state)
                    score = sum(a for a,h,hh in zip(attacks,hps,after) if h > 0 >= hh)*100
                    score += sum(a-b for a,b in zip(hps,after))*8 + nxt - price*2
                    score += sum(a-b for a,b,h in zip(attacks,reduced,after) if h > 0)*60
                    step = (chr(97+p%5)+str(p//5+1),name,direction,nxt)
                    candidates.append((score,state,step))
        candidates.sort(key=lambda a:a[0],reverse=True)
        for _,state,step in candidates:
            rest = visit(*state)
            if rest is not None:
                return [step]+rest
        return None

    try:
        result = visit(tuple(e[3] for e in enemies),tuple(e[2] for e in enemies),hp,0,cost)
        return dict(status='SOLVED' if result is not None else 'NO_SOLUTION',nodes=nodes,steps=result)
    except TimeoutError:
        return dict(status='UNKNOWN',nodes=nodes,steps=None)


def main():
    rows = None
    if len(sys.argv) > 1:
        with Path(sys.argv[1]).open(encoding='utf-8-sig',newline='') as stream:
            rows = list(csv.reader(stream))
    results = []
    for n in (16,17,18):
        hp,cost,enemies,units = load(n)
        if rows is not None:
            index = next(i for i,row in enumerate(rows) if row[0]=='マップNo' and row[1].strip(',')==str(n))
            start = index-2
            assert (hp,cost) == (int(rows[start+1][8]),int(rows[start+3][8]))
            assert [u[0] for u in units] == rows[index][2].split(',')
            expected = []
            for y in range(5):
                for x,cell in enumerate(rows[start+y][3:8]):
                    if cell != 'b':
                        expected.append((x,y,*map(int,cell.split('/'))))
            assert enemies == expected
        result = dict(stage=n,**solve(hp,cost,enemies,units))
        print(json.dumps(result),flush=True)
        if result['status']=='NO_SOLUTION':
            print(json.dumps(dict(stage=n,hp_plus_one=solve(hp+1,cost,enemies,units))),flush=True)
        results.append(result)
    Path('Logs').mkdir(exist_ok=True)
    Path('Logs/debuff-map-solutions.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
    if any(result['status'] != 'SOLVED' for result in results):
        sys.exit(1)


if __name__=='__main__':
    main()
