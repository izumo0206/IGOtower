"""Check map19-22 against CSV and search with persistent D/E attacks.

Not a Unity runtime test. Coordinates use a-e and top-to-bottom rows 1-5.
One placement or pass per turn; D/E trigger before enemy retaliation, including
on their placement turn. Passing is allowed even after spending all cost.
"""
import argparse
import csv
import json
import time
from pathlib import Path

from verify_value_maps import ROOT, RES, field, load


def solve(hp, cost, enemies, units, seconds=20):
    assert all(skill in (1, 2, 4, 5, 6) and timing in (1, 2)
               for _, _, _, skill, timing in units)
    assert all(skill in (1, 2) for _, _, _, skill, timing in units if timing == 2)
    positions = [y * 5 + x for x, y, _, _ in enemies]
    neighbors = [tuple(yy * 5 + xx for xx, yy in
                       ((x-1, y), (x+1, y), (x, y-1), (x, y+1))
                       if 0 <= xx < 5 and 0 <= yy < 5)
                 for x, y, _, _ in enemies]
    masks = [sum(1 << p for p in nn) for nn in neighbors]
    directions = ((0, -1, 'up'), (1, 0, 'right'), (0, 1, 'down'), (-1, 0, 'left'))
    effects = {}
    for p in range(25):
        for name, _, _, skill, _ in units:
            options = []
            for dx, dy, direction in (directions if skill in (1, 5) else ((0, 0, 'none'),)):
                targets = tuple(i for i, (x, y, _, _) in enumerate(enemies)
                                if ((x, y) == (p % 5 + dx, p // 5 + dy)
                                    if skill in (1, 5) else p in neighbors[i]))
                options.append((direction, targets))
            effects[p, name] = options
    started = time.monotonic()
    seen = {}
    nodes = 0

    def visit(hps, attacks, dots, health, occupied, remaining):
        nonlocal nodes
        nodes += 1
        if nodes % 128 == 0 and time.monotonic() - started > seconds:
            raise TimeoutError
        if not any(h > 0 for h in hps):
            return []
        key = (hps, attacks, dots, occupied, remaining)
        if seen.get(key, -1) >= health:
            return None
        seen[key] = health
        candidates = []
        outcomes = set()

        def finish(after, reduced, rates, healed, placed, left, step):
            # Only D/E have end-of-turn skills here, so their damage commutes.
            after = tuple(max(0, h - rate) for h, rate in zip(after, rates))
            reduced = tuple(a if h > 0 else 0 for a, h in zip(reduced, after))
            rates = tuple(rate if h > 0 else 0 for rate, h in zip(rates, after))
            nxt = healed - sum(reduced)
            if nxt <= 0:
                return
            state = (after, reduced, rates, nxt, placed, left)
            if state in outcomes:
                return
            outcomes.add(state)
            score = sum(a for a, h, hh in zip(attacks, hps, after) if h > 0 >= hh) * 100
            score += sum(a-b for a, b in zip(hps, after)) * 8 + nxt
            score += sum(a-b for a, b, h in zip(attacks, reduced, after) if h > 0) * 60
            score += sum(rates) * 5 + left * 2
            candidates.append((score, state, (*step, nxt)))

        for p in range(25):
            if occupied & (1 << p) or any(h > 0 and e == p for e, h in zip(positions, hps)):
                continue
            walls = int(p % 5 in (0, 4)) + int(p // 5 in (0, 4))
            if walls + sum(h > 0 and p in nn for h, nn in zip(hps, neighbors)) == 4:
                continue
            for name, price, power, skill, timing in units:
                if price > remaining:
                    continue
                for direction, targets in effects[p, name]:
                    after, reduced, rates = list(hps), list(attacks), list(dots)
                    healed = health
                    if timing == 1 and skill == 4:
                        healed += power
                    for i in targets:
                        if after[i] <= 0:
                            continue
                        if timing == 2 and skill in (1, 2):
                            rates[i] += power
                        elif timing == 1 and skill in (1, 2):
                            after[i] = max(0, after[i] - power)
                        elif timing == 1 and skill in (5, 6):
                            reduced[i] = max(0, reduced[i] - power)
                    placed = occupied | (1 << p)
                    # RegisterUnit checks adjacent enemies after OnPlaced, before
                    # turn-end damage. A neighboring enemy prevents capture.
                    for i, h in enumerate(after):
                        if h > 0 and p in neighbors[i] and placed & masks[i] == masks[i]:
                            after[i] = 0
                    step = (chr(97 + p % 5) + str(p // 5 + 1), name, direction)
                    finish(after, reduced, rates, healed, placed, remaining-price, step)
        # No change without ongoing damage: such a pass cannot improve survival.
        if any(rate > 0 and h > 0 for rate, h in zip(dots, hps)):
            finish(hps, attacks, dots, health, occupied, remaining, ('pass', '-', 'none'))
        candidates.sort(key=lambda action: action[0], reverse=True)
        for _, state, step in candidates:
            rest = visit(*state)
            if rest is not None:
                return [step] + rest
        return None

    try:
        result = visit(tuple(e[3] for e in enemies), tuple(e[2] for e in enemies),
                       (0,) * len(enemies), hp, 0, cost)
        return dict(status='SOLVED' if result is not None else 'NO_SOLUTION', nodes=nodes, steps=result)
    except TimeoutError:
        return dict(status='UNKNOWN', nodes=nodes, steps=None)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('stages', nargs='*', type=int)
    parser.add_argument('--csv', type=Path)
    parser.add_argument('--seconds', type=float, default=20)
    parser.add_argument('--exclude', nargs='+', default=[])
    args = parser.parse_args()
    manifest = json.loads((ROOT / 'Docs/持続ダメージマップ_データ.json').read_text(encoding='utf-8'))
    requested = set(args.stages) if args.stages else {entry['stage'] for entry in manifest}
    assert requested and requested <= {entry['stage'] for entry in manifest}, 'Unknown stage requested'
    assert set(args.exclude) <= set('ACDEGH'), 'Unknown ally excluded'
    for filename, name, power, skill in [('004', 'D', 2, 1), ('005', 'E', 1, 2)]:
        data = (RES / f'AllyUnit/{filename}.asset').read_text(encoding='utf-8-sig')
        assert field(data, 'unitName') == name
        assert tuple(int(field(data, key)) for key in ('cost', 'attackPower', 'skillTiming', 'skillType')) == (2, power, 2, skill)
    rows = None
    if args.csv:
        with args.csv.open(encoding='utf-8-sig', newline='') as stream:
            rows = list(csv.reader(stream))
    results = []
    for entry in manifest:
        n = entry['stage']
        if n not in requested:
            continue
        hp, cost, enemies, units = load(n, include_timing=True)
        assert (hp, cost, [list(e) for e in enemies], [u[0] for u in units]) == (
            entry['hp'], entry['cost'], entry['enemies'], entry['allies']), f'Saved data mismatch: map{n}'
        if rows is not None:
            index = next(i for i, row in enumerate(rows)
                         if row[0] == 'マップNo' and row[1].strip(',') == str(n))
            start = index - 2
            assert (hp, cost) == (int(rows[start+1][8]), int(rows[start+3][8]))
            assert [u[0] for u in units] == rows[index][2].split(',')
            expected = [(x, y, *map(int, cell.split('/')))
                        for y in range(5) for x, cell in enumerate(rows[start+y][3:8]) if cell != 'b']
            assert enemies == expected, f'CSV mismatch: map{n}'
        result = dict(stage=n, **solve(hp, cost, enemies,
                      [u for u in units if u[0] not in args.exclude], args.seconds))
        results.append(result)
        print(json.dumps(result), flush=True)
    if not args.exclude and requested == {entry['stage'] for entry in manifest}:
        (ROOT / 'Logs').mkdir(exist_ok=True)
        (ROOT / 'Logs/sustained-map-solutions.json').write_text(json.dumps(results, indent=2) + '\n', encoding='utf-8')
    if any(result['status'] != 'SOLVED' for result in results):
        raise SystemExit(1)


if __name__ == '__main__':
    main()
