"""Verify learning map data and find A/C solutions (not a Unity runtime test).

Run: python Tools/verify_learning_maps.py [source.csv]
Coordinates in output: columns a-e, rows 1-5 from the top of the board.
"""
import csv
import json
import re
import sys
from functools import lru_cache
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "Assets/Resources"


def number(text, key):
    return int(re.search(rf"^  {key}: (\d+)$", text, re.M)[1])


def read_map(n):
    lines = (RES / f"Stage/map{n}.txt").read_text(encoding="utf-8-sig").splitlines()
    head = lines[0].split()
    grid = [list(map(int, row.split())) for row in lines[1:]][::-1]
    assert head[2:] == ["allies=001,003"]
    assert len(grid) == 5 and all(len(row) == 5 for row in grid)
    assert all(v >= 1 for row in grid for v in row), "b means a valid empty tile"
    enemies = []
    for y, row in enumerate(grid):
        for x, value in enumerate(row):
            if value > 1:
                data = (RES / f"EnemyUnit/{value}.asset").read_text(encoding="utf-8-sig")
                enemies.append((x, y, number(data, "attackPower"), number(data, "maxHp")))
    return int(head[0]), int(head[1]), enemies


def solve(initial_hp, budget, enemies, first_kill=None):
    positions = [y * 5 + x for x, y, _, _ in enemies]
    neighbors = []
    for x, y, _, _ in enemies:
        neighbors.append(tuple(yy * 5 + xx for xx, yy in
                               ((x+1, y), (x-1, y), (x, y+1), (x, y-1))
                               if 0 <= xx < 5 and 0 <= yy < 5))

    @lru_cache(None)
    def visit(hps, hp, occupied, left):
        if not any(h > 0 for h in hps):
            return ()
        if not left:
            return None
        for p in range(25):
            if occupied & (1 << p) or any(h > 0 and q == p for h, q in zip(hps, positions)):
                continue
            targets = [i for i, h in enumerate(hps) if h > 0 and p in neighbors[i]]
            # -1: A aimed away, -2: C. All directions with the same outcome collapse.
            for target in targets + [-1, -2]:
                after = list(hps)
                health = hp + (3 if target == -2 else 0)  # no initial-HP cap
                if target >= 0:
                    after[target] -= 2
                placed = occupied | (1 << p)
                for i, h in enumerate(after):
                    if h > 0 and p in neighbors[i] and all(placed & (1 << q) for q in neighbors[i]):
                        after[i] = 0
                killed = [i for i, (a, b) in enumerate(zip(hps, after)) if a > 0 >= b]
                if first_kill is not None and all(h > 0 for h in hps) and killed and first_kill not in killed:
                    continue
                health -= sum(e[2] for e, h in zip(enemies, after) if h > 0)
                if health <= 0:
                    continue
                rest = visit(tuple(after), health, placed, left - 1)
                if rest is not None:
                    return ((chr(97 + p % 5) + str(p // 5 + 1),
                             "C" if target == -2 else "A", target, health),) + rest
        return None

    # Passing cannot help with A/C only: it adds damage without changing the board.
    return visit(tuple(e[3] for e in enemies), initial_hp, 0, budget)


def main():
    for filename, skill, power in [("001", 1, 2), ("003", 4, 3)]:
        data = (RES / f"AllyUnit/{filename}.asset").read_text(encoding="utf-8-sig")
        assert (number(data, "cost"), number(data, "skillTiming"),
                number(data, "skillType"), number(data, "attackPower")) == (1, 1, skill, power)
    expected = {
        1: (3, 2, [(1, 2, 2, 2), (3, 2, 2, 2)]),
        2: (2, 4, [(2, 2, 2, 8)]),
        3: (2, 3, [(2, 0, 3, 6)]),
        4: (3, 2, [(1, 2, 4, 2), (3, 2, 2, 2)]),
        5: (12, 5, [(2, 0, 3, 5), (2, 3, 5, 4)]),
        6: (17, 5, [(2, 0, 5, 8), (1, 3, 3, 2), (4, 3, 2, 2)]),
    }
    if len(sys.argv) > 1:
        with open(sys.argv[1], encoding="utf-8-sig", newline="") as stream:
            rows = list(csv.reader(stream))
        for n, start, column in [(1, 1, 3), (2, 1, 9), (3, 6, 3), (4, 11, 3), (5, 16, 3)]:
            hp, cost, enemies = expected[n]
            assert int(rows[start + 1][column + 5]) == hp
            assert int(rows[start + 3][column + 5]) == cost
            from_csv = []
            for y in range(5):
                for x in range(5):
                    cell = rows[start + y][column + x]
                    if cell != "b":
                        attack, enemy_hp = map(int, cell.split("/"))
                        from_csv.append((x, y, attack, enemy_hp))
            assert from_csv == enemies, f"CSV mismatch: map{n}"
    for n, spec in expected.items():
        actual = read_map(n)
        assert actual == spec, f"Saved data mismatch: map{n}"
        solution = solve(*actual)
        assert solution is not None, f"No solution: map{n}"
        assert solve(actual[0], actual[1] - 1, actual[2]) is None, f"Unexpected shorter solution: map{n}"
        print(json.dumps({"map": n, "solution": solution}, ensure_ascii=False))
    for n, wrong in [(4, 1), (5, 0), (6, 0), (6, 2)]:
        assert solve(*read_map(n), first_kill=wrong) is None, f"Unexpected first kill: map{n}/{wrong}"
    print("PASS: saved data, six solutions, minimum placement budgets, first-kill constraints.")


if __name__ == "__main__":
    main()
