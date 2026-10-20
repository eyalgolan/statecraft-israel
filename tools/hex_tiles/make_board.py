#!/usr/bin/env python3
"""Generate a hex-tile board (Civ-style map) for a region of the game map.

Reads the game's own hexes (content/map/hexes.json) and 1948 settlements (content/map/settlements_1948.yaml) and writes
unity/Assets/Resources/HexTiles/Boards/<id>.json for the hex-tile layer (docs/hex-tiles/README.md):

- terrain by elevation, game-friendly like Civ: flat below 120 m, hills to 450 m, smooth highland "mountain" above
  (the real cliffs and ravines are not reproduced; units cross every land hex);
- ground layer by biome;
- tile prototype per hex: each settlement's own (city, old city, town, village, kibbutz), otherwise one chosen by
  terrain, biome and the coast with a stable per-hex draw;
- roads between the main towns along hex lines, the Yarkon along hex edges, and the board's standing units.

The output is a plain file: Tal edits it by hand after generating, and re-running overwrites it.

    python3 tools/hex_tiles/make_board.py [--id central_israel] [--lat 31.62 32.48] [--lon 34.4 35.42]
"""
import argparse, json, math, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
NEIGHBOURS = [(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)]


def hex_distance(a, b):
    dq, dr = b[0] - a[0], b[1] - a[1]
    return (abs(dq) + abs(dr) + abs(dq + dr)) // 2


def hex_line(a, b):
    """Axial hexes from a to b inclusive (cube lerp and round)."""
    n = hex_distance(a, b)
    out = []
    for i in range(n + 1):
        t = i / n if n else 0.0
        q = a[0] + (b[0] - a[0]) * t + 1e-6
        r = a[1] + (b[1] - a[1]) * t + 1e-6
        s = -q - r
        rq, rr, rs = round(q), round(r), round(s)
        dq, dr, ds = abs(rq - q), abs(rr - r), abs(rs - s)
        if dq > dr and dq > ds: rq = -rr - rs
        elif dr > ds: rr = -rq - rs
        if not out or out[-1] != (rq, rr): out.append((rq, rr))
    return out


def settlements(path):
    """id, lon, lat, type from the YAML list (simple keys only, no YAML dependency)."""
    out, cur = [], None
    for line in open(path, encoding='utf-8'):
        m = re.match(r'^- id:\s*(\S+)', line)
        if m:
            cur = {'id': m.group(1)}; out.append(cur); continue
        m = re.match(r'^\s+(lon|lat|type):\s*(\S+)', line)
        if m and cur is not None:
            cur[m.group(1)] = float(m.group(2)) if m.group(1) in ('lon', 'lat') else m.group(2)
    return [s for s in out if 'lon' in s and 'lat' in s]


def draw(q, r, salt):
    """A stable value in [0, 1) per hex (no RNG state)."""
    x = (q * 73856093) ^ (r * 19349663) ^ (salt * 83492791)
    x = (x ^ (x >> 13)) * 1274126177 & 0xFFFFFFFF
    return ((x ^ (x >> 16)) & 0xFFFFFF) / float(1 << 24)


# A settlement's own tile: by id first, then by type. Jewish towns read as moshava colonies, Arab towns as stone villages.
SETTLEMENT_TILE = {'tel_aviv': 'tel_aviv_city', 'jaffa': 'jaffa_old_city', 'jerusalem': 'jerusalem_old_city'}
TYPE_TILE = {'city': 'moshava_village', 'town': 'moshava_village', 'arab_town': 'arab_village', 'kibbutz': 'kibbutz', 'moshav': 'kibbutz'}


def terrain_of(h):
    if not h['land']: return 'water'
    e = h.get('elev_m', 0.0)
    if e >= 450: return 'mountain'
    if e >= 120 or h['biome'] == 'hills': return 'hills'
    return 'flat'


GROUND = {'coastal_plain': 'grass', 'valley': 'plains', 'hills': 'hills', 'mountains': 'mountain', 'desert': 'desert'}


def nature_tile(h, terrain, coastal):
    q, r, b = h['q'], h['r'], h['biome']
    u = draw(q, r, 1)
    if b == 'desert': return 'desert_edge'
    if terrain == 'mountain': return 'judean_highlands' if u < 0.75 else ('pine_forest' if u < 0.9 else 'olive_terraces')
    if terrain == 'hills': return 'olive_terraces' if u < 0.45 else ('pine_forest' if u < 0.7 else 'grazing_land')
    if coastal and u < 0.45: return 'coastal_dunes'
    if b == 'valley': return 'wheat_fields' if u < 0.6 else ('grazing_land' if u < 0.85 else 'citrus_orchard')
    return 'wheat_fields' if u < 0.42 else ('citrus_orchard' if u < 0.75 else 'grazing_land')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--id', default='central_israel')
    ap.add_argument('--lat', nargs=2, type=float, default=[31.62, 32.48])
    ap.add_argument('--lon', nargs=2, type=float, default=[34.40, 35.42])
    ap.add_argument('--sea-ring', type=int, default=1, help='sea hexes kept within this many hexes of land')
    ap.add_argument('--around', help='crop to hexes around this settlement id instead of a lat/lon box')
    ap.add_argument('--radius', type=int, default=3, help='with --around: playable hexes within this many steps')
    ap.add_argument('--env', type=int, default=0, help='with --around: a ring this many hexes wide of environment (landscape only, no tiles or hex lines)')
    a = ap.parse_args()

    H = json.load(open(os.path.join(ROOT, 'content/map/hexes.json')))['hexes']
    by = {(h['q'], h['r']): h for h in H}
    if a.around:
        home = next(s for s in settlements(os.path.join(ROOT, 'content/map/settlements_1948.yaml')) if s['id'] == a.around)
        c = min(H, key=lambda h: (h['lon'] - home['lon']) ** 2 + (h['lat'] - home['lat']) ** 2)
        reach = a.radius + a.env
        inside = [h for h in H if hex_distance((h['q'], h['r']), (c['q'], c['r'])) <= reach]
        lats = [h['lat'] for h in inside]; lons = [h['lon'] for h in inside]
        a.lat, a.lon = [min(lats) - 0.01, max(lats) + 0.01], [min(lons) - 0.01, max(lons) + 0.01]
        env_of = lambda k: hex_distance(k, (c['q'], c['r'])) > a.radius
    else:
        inside = [h for h in H if a.lat[0] <= h['lat'] <= a.lat[1] and a.lon[0] <= h['lon'] <= a.lon[1]]
        env_of = lambda k: False
    land = {(h['q'], h['r']) for h in inside if h['land']}
    keep = [h for h in inside if h['land'] or any(hex_distance((h['q'], h['r']), k) <= a.sea_ring for k in land)]
    cells = {(h['q'], h['r']): h for h in keep}

    def nearest(lon, lat):
        return min(cells.values(), key=lambda h: (h['lon'] - lon) ** 2 + (h['lat'] - lat) ** 2)

    # Settlements first: a city outranks a town, a town a village; a second settlement on a taken hex moves to the
    # nearest free land neighbour (Jaffa beside Tel Aviv).
    rank = {'city': 0, 'town': 1, 'arab_town': 1, 'moshav': 2, 'kibbutz': 2}
    owner = {}
    for s in sorted(settlements(os.path.join(ROOT, 'content/map/settlements_1948.yaml')), key=lambda s: (rank.get(s.get('type'), 3), s['id'])):
        if not (a.lat[0] <= s['lat'] <= a.lat[1] and a.lon[0] <= s['lon'] <= a.lon[1]): continue
        h = nearest(s['lon'], s['lat']); k = (h['q'], h['r'])
        if env_of(k): continue
        if k in owner or not h['land']:
            free = [(k[0] + dq, k[1] + dr) for dq, dr in NEIGHBOURS]
            free = [f for f in free if f in cells and cells[f]['land'] and f not in owner]
            if not free: continue
            k = min(free, key=lambda f: (cells[f]['lon'] - s['lon']) ** 2 + (cells[f]['lat'] - s['lat']) ** 2)
        owner[k] = s

    out = []
    for k, h in sorted(cells.items(), key=lambda kv: (kv[0][1], kv[0][0])):
        terrain = terrain_of(h)
        e = {'q': k[0], 'r': k[1], 'terrain': terrain, 'ground': 'seabed' if terrain == 'water' else GROUND.get(h['biome'], 'grass'),
             'prototype': '', 'note': '', 'unit': '', 'facing': 0.0, 'env': env_of(k)}
        if e['env']:
            # Environment: terrain and ground only; the builder dresses it as continuous landscape (no tiles, no hex lines).
            if terrain == 'mountain': e['terrain'] = 'hills'
            out.append(e); continue
        if terrain != 'water':
            coastal = any((k[0] + dq, k[1] + dr) in by and not by[(k[0] + dq, k[1] + dr)]['land'] for dq, dr in NEIGHBOURS)
            if k in owner:
                s = owner[k]
                e['prototype'] = SETTLEMENT_TILE.get(s['id'], TYPE_TILE.get(s.get('type'), 'moshava_village'))
                e['note'] = s['id']
                if e['prototype'] in ('tel_aviv_city', 'moshava_village', 'kibbutz') and terrain == 'mountain': e['terrain'] = 'hills'
            else:
                e['prototype'] = nature_tile(h, terrain, coastal)
                if e['prototype'] == 'coastal_dunes': e['ground'] = 'beach'
            if e['prototype'] in ('wheat_fields', 'citrus_orchard', 'grazing_land', 'moshava_village', 'kibbutz', 'tel_aviv_city', 'jaffa_old_city', 'coastal_dunes') and terrain == 'mountain':
                e['terrain'] = 'hills'
        out.append(e)

    # Roads: Civ-style, hex centre to hex centre, between the main towns (drawn along hex lines).
    hex_of = {s['id']: k for k, s in owner.items()}
    routes = [('tel_aviv', 'lod'), ('lod', 'ramla'), ('ramla', 'jerusalem'), ('tel_aviv', 'petah_tikva'), ('tel_aviv', 'rishon_lezion'),
              ('rishon_lezion', 'rehovot'), ('tel_aviv', 'netanya'), ('netanya', 'hadera'), ('jaffa', 'tel_aviv')]
    roads = []
    for a_id, b_id in routes:
        if a_id in hex_of and b_id in hex_of:
            line = [p for p in hex_line(hex_of[a_id], hex_of[b_id]) if p in cells and cells[p]['land']]
            line = [p for p in line if not env_of(p)]
            if len(line) >= 2: roads.append({'note': f'{a_id} to {b_id}', 'hexes': [v for p in line for v in p]})
    rivers = []
    ta = hex_of.get('tel_aviv')
    if ta:   # the Yarkon: along the north edges of Tel Aviv's row, out to the sea
        q, r = ta
        rivers.append({'note': 'The Yarkon', 'corners': [q - 1, r, 0, q, r, 5, q, r, 0, q, r, 1, q + 1, r, 0, q + 1, r, 1, q + 2, r, 0, q + 2, r, 1, q + 3, r, 0]})

    # Units standing on the board (a layer above the tiles): a squad by Tel Aviv, the armoured truck on the Jerusalem road.
    def stand(unit, near, facing):
        if near in hex_of:
            k = hex_of[near]
            for dq, dr in NEIGHBOURS:
                f = (k[0] + dq, k[1] + dr)
                e = next((x for x in out if (x['q'], x['r']) == f), None)
                if e and e['terrain'] != 'water' and not e['unit'] and not e['env'] and e['prototype'] not in ('tel_aviv_city', 'jerusalem_old_city', 'jaffa_old_city'):
                    e['unit'], e['facing'] = unit, facing; return
    stand('palmach_squad', 'tel_aviv', 40.0)
    stand('sandwich_truck', 'ramla', 100.0)
    stand('willys_jeep', 'jerusalem', 270.0)

    board = {'id': a.id, 'title': f'{a.id} (generated by tools/hex_tiles/make_board.py; lat {a.lat}, lon {a.lon})', 'hexes': out, 'roads': roads, 'rivers': rivers}
    dst = os.path.join(ROOT, 'unity/Assets/Resources/HexTiles/Boards', a.id + '.json')
    json.dump(board, open(dst, 'w'), indent=1)
    from collections import Counter
    print(f'{dst}: {len(out)} hexes ({sum(1 for e in out if e["terrain"] != "water")} land), {len(roads)} roads, settlements {len(owner)}')
    print(' terrain', dict(Counter(e['terrain'] for e in out)))
    print(' tiles', dict(Counter(e['prototype'] for e in out if e['prototype'])))


if __name__ == '__main__':
    sys.exit(main())
