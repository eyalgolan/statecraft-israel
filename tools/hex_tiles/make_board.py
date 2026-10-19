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

With --regions (tools/hex_tiles/regions.yaml) the choices come from region recipes instead: each region (a lat/lon
polygon or a list of hexes) maps terrain, biome, settlement type and population tier to a prototype and a ground
layer, and names the trees and rocks of its environment hexes. The settlements are then the game's 25 plus the 1948
localities (content/map/localities_1948.tsv), and the roads follow the traced 1948 main roads
(content/map/route_paths_1948.tsv). One command lays out the whole country:

    python3 tools/hex_tiles/make_board.py --regions tools/hex_tiles/regions.yaml --id israel --all --env-ring 1

Hand edits that must survive regeneration go in the sibling <id>.overrides.json (the Hex Painter writes it), applied
last. It has the shape of the map editor's content/map/overrides.yaml (#1099): one list per field (prototype, ground,
unit, facing, region) of {hex: {q, r}, value}. It is presentation only and stays out of content/, so painting a hex never
moves the content hash. The map editor's own terrain and location overrides are read too, so boards follow the map.

    python3 tools/hex_tiles/make_board.py [--id central_israel] [--lat 31.62 32.48] [--lon 34.4 35.42]
"""
import argparse, json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BOARDS = os.path.join(ROOT, 'unity/Assets/Resources/HexTiles/Boards')
NEIGHBOURS = [(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)]
OVERRIDE_FIELDS = ('prototype', 'ground', 'unit', 'facing', 'region')


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


def populations(path):
    """id -> 1945 population from settlement_population_1945.yaml (simple keys only)."""
    out, cur = {}, None
    for line in open(path, encoding='utf-8'):
        m = re.match(r'^- id:\s*(\S+)', line)
        if m: cur = m.group(1); continue
        m = re.match(r'^\s+population:\s*(\d+)', line)
        if m and cur: out[cur] = int(m.group(1))
    return out


def localities(path):
    """The 1948 places of localities_1948.tsv (its 'place' rows; the camp rows have no point): id, lon, lat, type,
    population."""
    out, cols = [], None
    for line in open(path, encoding='utf-8'):
        row = line.rstrip('\n').split('\t')
        if row[0] == '#place': cols = row; continue
        if row[0] != 'place' or cols is None: continue
        d = dict(zip(cols, row))
        out.append({'id': d['id'], 'lon': float(d['lon']), 'lat': float(d['lat']), 'type': d['type'] or 'village',
                    'population': int(d['population'] or 0)})
    return out


def draw(q, r, salt):
    """A stable value in [0, 1) per hex (no RNG state)."""
    x = (q * 73856093) ^ (r * 19349663) ^ (salt * 83492791)
    x = (x ^ (x >> 13)) * 1274126177 & 0xFFFFFFFF
    return ((x ^ (x >> 16)) & 0xFFFFFF) / float(1 << 24)


# A settlement's own tile: by id first, then by type. Jewish towns read as moshava colonies, Arab towns as stone villages.
SETTLEMENT_TILE = {'tel_aviv': 'tel_aviv_city', 'jaffa': 'jaffa_old_city', 'jerusalem': 'jerusalem_old_city'}
TYPE_TILE = {'city': 'moshava_village', 'town': 'moshava_village', 'arab_town': 'arab_village', 'kibbutz': 'kibbutz', 'moshav': 'kibbutz'}
# Tiles that want low ground: on a mountain hex they lower it to hills.
LOWLAND = ('wheat_fields', 'citrus_orchard', 'grazing_land', 'moshava_village', 'kibbutz', 'tel_aviv_city', 'jaffa_old_city', 'coastal_dunes')


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


def _yaml(path):
    """YAML 1.2 core schema, as every repo tool reads YAML (tools/yaml12.py; needs pyyaml, which CI's tools tests have)."""
    sys.path.insert(0, os.path.join(ROOT, 'tools'))
    import yaml12
    return yaml12.load_path(path)


# --- Region recipes ---------------------------------------------------------------------------------------------------

def in_polygon(lon, lat, poly):
    """Even-odd rule; poly is [[lon, lat], ...]."""
    inside, n = False, len(poly)
    for i in range(n):
        x0, y0 = poly[i]; x1, y1 = poly[(i + 1) % n]
        if (y0 > lat) != (y1 > lat) and lon < x0 + (lat - y0) * (x1 - x0) / (y1 - y0): inside = not inside
    return inside


class Recipes:
    """regions.yaml: the defaults and the regions, in file order. A hex is in the first region that lists it by q/r,
    else the first whose polygon holds its centre, else the default region."""

    def __init__(self, data):
        self.defaults = data.get('defaults', {})
        self.regions = data['regions']
        self.by_id = {r['id']: r for r in self.regions}
        self.order = [r['id'] for r in self.regions]
        self.default_region = self.defaults.get('region', self.order[-1])
        self.listed = {}
        for reg in self.regions:
            hs = reg.get('hexes', [])
            for i in range(0, len(hs), 2): self.listed.setdefault((hs[i], hs[i + 1]), reg['id'])
        self.tiers = sorted(self.defaults.get('tiers', {'village': 0}).items(), key=lambda kv: -kv[1])

    @staticmethod
    def load(path):
        return Recipes(_yaml(path))

    def region_of(self, h):
        k = (h['q'], h['r'])
        if k in self.listed: return self.listed[k]
        for reg in self.regions:
            if 'polygon' in reg and in_polygon(h['lon'], h['lat'], reg['polygon']): return reg['id']
        return self.default_region

    def tier_of(self, population):
        for name, least in self.tiers:
            if (population or 0) >= least: return name
        return self.tiers[-1][0]

    def rules(self, region, key):
        """The region's rules for key (settlements or nature), then the defaults'."""
        return list(self.by_id[region].get(key, [])) + list(self.defaults.get(key, []))

    def ground(self, region, biome):
        table = dict(self.defaults.get('ground', GROUND)); table.update(self.by_id[region].get('ground', {}))
        return table.get(biome, table.get('default', 'grass'))


def _matches(rule, **facts):
    for key, value in facts.items():
        if key not in rule: continue
        want = rule[key] if isinstance(rule[key], list) else [rule[key]]
        if value not in want: return False
    return True


def _pick(rule, q, r):
    """A rule's prototype, or a weighted pick from its 'pick' list with the hex's stable draw."""
    if 'prototype' in rule: return rule['prototype']
    total = sum(w for _, w in rule['pick'])
    u, acc = draw(q, r, 1) * total, 0.0
    for proto, w in rule['pick']:
        acc += w
        if u < acc: return proto
    return rule['pick'][-1][0]


def apply_rules(rules, q, r, **facts):
    """The first rule whose keys all match the facts: its prototype (or pick) and its ground, if it sets one."""
    return apply_rule(rules, q, r, **facts)[:2]


def apply_rule(rules, q, r, **facts):
    """apply_rules, plus the rule's `set_terrain` (a presentation terrain the tile wants, as a valley floor whose
    average height reads as hills wants flat), or None."""
    for rule in rules:
        if _matches(rule, **facts): return _pick(rule, q, r), rule.get('ground'), rule.get('set_terrain')
    raise ValueError(f'no rule matches hex ({q},{r}) {facts}')


def flora_entry(region):
    """The board's copy of a region's environment dress (BoardRegion in HexBoard.cs)."""
    f = region.get('flora', {})
    def species(items): return [{'model': s['model'], 'shape': s.get('shape', 'tree'), 'weight': float(s.get('weight', 1.0))} for s in items]
    rock = f.get('rock', {})
    return {'id': region['id'], 'title': region.get('title', region['id']), 'density': float(f.get('density', 1.0)),
            'flat': species(f.get('flat', [])), 'hills': species(f.get('hills', [])),
            'rock': rock.get('model', 'sandbox:limestone*'), 'rockChance': float(rock.get('chance', 0.6)), 'rocksOnFlat': bool(rock.get('on_flat', False))}


def route_roads(cells, kinds, before):
    """Roads along the traced 1948 routes (route_paths_1948.tsv): each route's points snapped to their nearest board
    land hex, gaps joined along hex lines; only routes of the given kinds that the map draws before the given date."""
    roads = []
    land = {k: h for k, h in cells.items() if h['land']}
    for line in open(os.path.join(ROOT, 'content/map/route_paths_1948.tsv'), encoding='utf-8'):
        row = line.rstrip('\n').split('\t')
        if row[0] != 'route' or row[2] not in kinds or (row[4] and row[4] > before): continue
        hexes = []
        def flush():
            if len(hexes) >= 2: roads.append({'note': row[1], 'hexes': [v for p in hexes for v in p]})
        for pt in row[-1].split(';'):
            lon, lat = map(float, pt.split())
            k = min(land, key=lambda k: (land[k]['lon'] - lon) ** 2 + (land[k]['lat'] - lat) ** 2)
            if (land[k]['lon'] - lon) ** 2 + (land[k]['lat'] - lat) ** 2 > 0.05 ** 2:   # off the board
                flush(); hexes = []; continue
            if hexes and hexes[-1] == k: continue
            if hexes and hex_distance(hexes[-1], k) > 1:
                hexes += [p for p in hex_line(hexes[-1], k)[1:-1] if p in land]
            hexes.append(k)
        flush()
    return roads


# --- Overrides --------------------------------------------------------------------------------------------------------

def overrides_path(board_path):
    """<board>.overrides.json beside <board>.json."""
    return (board_path[:-len('.json')] if board_path.endswith('.json') else board_path) + '.overrides.json'


def apply_overrides(board, overrides):
    """A board's hand edits, in the shape of content/map/overrides.yaml (#1099): one list per field (prototype, ground,
    unit, facing, region) of {hex: {q, r}, value}, no hex twice in a list; facing is degrees as a string. Returns the
    edits whose hex the board does not have (reported, not applied)."""
    at = {(e['q'], e['r']): e for e in board['hexes']}
    unknown = [k for k in overrides if k != 'board' and k not in OVERRIDE_FIELDS]
    if unknown: raise ValueError(f"override list '{unknown[0]}' is not one of {OVERRIDE_FIELDS}")
    missing = []
    for field in OVERRIDE_FIELDS:
        seen = set()
        for ed in overrides.get(field, []):
            k = (ed['hex']['q'], ed['hex']['r'])
            if k in seen: raise ValueError(f'a second {field} override of hex {k}')
            seen.add(k)
            e = at.get(k)
            if e is None: missing.append((field, ed)); continue
            e[field] = float(ed['value']) if field == 'facing' else ed['value']
    return missing


def map_overrides(path=None):
    """The map editor's authored terrain and location moves (content/map/overrides.yaml, #1099/#1009), so a board
    follows the game map: {(q, r): biome} and {settlement id: (q, r)}. Control is the map's own and boards ignore it."""
    path = path or os.path.join(ROOT, 'content/map/overrides.yaml')
    if not os.path.exists(path): return {}, {}
    with open(path, encoding='utf-8') as f: text = f.read()
    # No terrain or location edits: nothing to read, so the built-in mode still runs without a YAML library.
    if all(re.search(rf'^{k}:\s*\[\]\s*$', text, re.M) for k in ('terrain', 'locations')): return {}, {}
    data = _yaml(path) or {}
    terrain = {(x['hex']['q'], x['hex']['r']): x['value'] for x in data.get('terrain') or []}
    locations = {x['id']: (x['hex']['q'], x['hex']['r']) for x in data.get('locations') or []}
    return terrain, locations


# --- The play area ----------------------------------------------------------------------------------------------------

def mandate_hexes(path=None):
    """The land hexes inside Mandatory Palestine on 15 May 1948: every hex of content/map/subdistrict_hexes_1948.yaml
    (the map's own sub-district layer, hex-snapped by tools/borders_bake.py from the sensitivity-reviewed true lines)
    whose region is not 'outside'. Read with a regex, so the built-in mode still needs no YAML library."""
    path = path or os.path.join(ROOT, 'content/map/subdistrict_hexes_1948.yaml')
    text = open(path, encoding='utf-8').read()
    outside = {int(m.group(1)) for m in re.finditer(r'^\s*- \{ id: (\d+), key: outside\b', text, re.M)}
    rows = re.finditer(r'^\s*- \[(-?\d+), (-?\d+), (\d+)\]', text, re.M)
    return {(int(m.group(1)), int(m.group(2))) for m in rows if int(m.group(3)) not in outside}


def clip_area(cells, mode, fringe, sea_ring):
    """The hexes a clipped board keeps and those it draws as environment. mode 'mandate': the land inside the Mandate
    sub-districts plays; land outside within `fringe` steps of it stays as environment (landscape with no tiles or hex
    lines, so the edge reads as land going on, not a cliff); water stays within `sea_ring` steps of playing land; the
    rest is dropped. Returns (kept keys, environment keys)."""
    if mode == 'none': return set(cells), set()
    if mode != 'mandate':
        raise ValueError(f"clip '{mode}' is not built: 'mandate' or 'none' (the 1949 armistice option waits on a design call)")
    inside = mandate_hexes() & {k for k, h in cells.items() if h['land']}
    def near(k, ring, of): return any(hex_distance(k, o) <= ring for o in of)
    # Only hexes within reach of the playing land need the distance test.
    reach = max(fringe, sea_ring)
    lo_q, hi_q = min(q for q, _ in inside) - reach, max(q for q, _ in inside) + reach
    lo_r, hi_r = min(r for _, r in inside) - reach, max(r for _, r in inside) + reach
    keep, env = set(inside), set()
    for k, h in cells.items():
        if k in inside or not (lo_q <= k[0] <= hi_q and lo_r <= k[1] <= hi_r): continue
        if h['land'] and fringe > 0 and near(k, fringe, inside): keep.add(k); env.add(k)
        elif not h['land'] and near(k, sea_ring, inside): keep.add(k)
    return keep, env


# --- The board --------------------------------------------------------------------------------------------------------

def parse(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('--id', default='central_israel')
    ap.add_argument('--lat', nargs=2, type=float, default=[31.62, 32.48])
    ap.add_argument('--lon', nargs=2, type=float, default=[34.40, 35.42])
    ap.add_argument('--all', action='store_true', help='every hex of the game map (instead of --lat/--lon)')
    ap.add_argument('--sea-ring', type=int, default=1, help='sea hexes kept within this many hexes of land')
    ap.add_argument('--around', help='crop to hexes around this settlement id instead of a lat/lon box')
    ap.add_argument('--radius', type=int, default=3, help='with --around: playable hexes within this many steps')
    ap.add_argument('--env', type=int, default=0, help='with --around: a ring this many hexes wide of environment (landscape only, no tiles or hex lines)')
    ap.add_argument('--env-ring', type=int, default=0, help='with a box or --all: land within this many hexes of the box edge is environment')
    ap.add_argument('--regions', help='region recipes (tools/hex_tiles/regions.yaml); without it, the built-in choices')
    ap.add_argument('--out', help='write here instead of unity/Assets/Resources/HexTiles/Boards/<id>.json')
    ap.add_argument('--map-overrides', help='the map editor\'s overrides (default content/map/overrides.yaml)')
    ap.add_argument('--clip', choices=['none', 'mandate'], help="the play area: 'mandate' keeps the land of Mandatory "
                    "Palestine (15 May 1948) and a fringe of environment around it; default: the recipes' "
                    "defaults.clip, else none")
    return ap.parse_args(argv)


def build_board(a):
    """The board as a dict, before overrides, and the settlement on each owned hex. Pure: the same arguments and
    inputs give the same board."""
    H = json.load(open(os.path.join(ROOT, 'content/map/hexes.json')))['hexes']
    # The map editor's terrain overrides set a land hex's biome, as the game map draws it.
    biomes, moved = map_overrides(getattr(a, 'map_overrides', None))
    for h in H:
        if (h['q'], h['r']) in biomes and h['land']: h['biome'] = biomes[(h['q'], h['r'])]
    by = {(h['q'], h['r']): h for h in H}
    recipes = Recipes.load(a.regions) if a.regions else None
    game = settlements(os.path.join(ROOT, 'content/map/settlements_1948.yaml'))
    if a.all:
        a.lat = [round(min(h['lat'] for h in H) - 0.01, 5), round(max(h['lat'] for h in H) + 0.01, 5)]
        a.lon = [round(min(h['lon'] for h in H) - 0.01, 5), round(max(h['lon'] for h in H) + 0.01, 5)]
    if a.around:
        home = next(s for s in game if s['id'] == a.around)
        c = min(H, key=lambda h: (h['lon'] - home['lon']) ** 2 + (h['lat'] - home['lat']) ** 2)
        reach = a.radius + a.env
        inside = [h for h in H if hex_distance((h['q'], h['r']), (c['q'], c['r'])) <= reach]
        lats = [h['lat'] for h in inside]; lons = [h['lon'] for h in inside]
        a.lat, a.lon = [min(lats) - 0.01, max(lats) + 0.01], [min(lons) - 0.01, max(lons) + 0.01]
        env_of = lambda k: hex_distance(k, (c['q'], c['r'])) > a.radius
    else:
        inside = [h for h in H if a.lat[0] <= h['lat'] <= a.lat[1] and a.lon[0] <= h['lon'] <= a.lon[1]]
        if a.env_ring > 0:
            # Environment: the hexes within env-ring steps of the box's edge (of a hex outside the box or off the map).
            extent = {(h['q'], h['r']) for h in inside}
            reach = [(dq, dr) for dq in range(-a.env_ring, a.env_ring + 1) for dr in range(-a.env_ring, a.env_ring + 1) if hex_distance((0, 0), (dq, dr)) <= a.env_ring]
            edge = {k for k in extent if any((k[0] + dq, k[1] + dr) not in extent for dq, dr in reach)}
            env_of = lambda k: k in edge
        else:
            env_of = lambda k: False
    land = {(h['q'], h['r']) for h in inside if h['land']}
    keep = [h for h in inside if h['land'] or any(hex_distance((h['q'], h['r']), k) <= a.sea_ring for k in land)]
    cells = {(h['q'], h['r']): h for h in keep}
    clip = getattr(a, 'clip', None) or (recipes.defaults.get('clip', {}).get('mode', 'none') if recipes else 'none')
    if clip != 'none':
        # The play area: the board keeps only the clipped land, its fringe and its sea (clip_area).
        fringe = int(recipes.defaults.get('clip', {}).get('fringe', 1)) if recipes else 1
        kept, fringe_env = clip_area(cells, clip, fringe, a.sea_ring)
        cells = {k: h for k, h in cells.items() if k in kept}
        box_env = env_of
        env_of = lambda k: k in fringe_env or box_env(k)
    in_box = lambda s: a.lat[0] <= s['lat'] <= a.lat[1] and a.lon[0] <= s['lon'] <= a.lon[1]

    def nearest(lon, lat):
        return min(cells.values(), key=lambda h: (h['lon'] - lon) ** 2 + (h['lat'] - lat) ** 2)

    # Settlements first: a city outranks a town, a town a village; a second settlement on a taken hex moves to the
    # nearest free land neighbour (Jaffa beside Tel Aviv).
    rank = {'city': 0, 'town': 1, 'arab_town': 1, 'moshav': 2, 'kibbutz': 2}
    owner = {}
    for s in sorted(game, key=lambda s: (rank.get(s.get('type'), 3), s['id'])):
        if not in_box(s): continue
        h = nearest(s['lon'], s['lat']); k = (h['q'], h['r'])
        if s['id'] in moved:   # moved by the map editor: that hex, as the game map has it
            k = moved[s['id']]
            if k not in cells or env_of(k): continue
            if k not in owner: owner[k] = s; continue
        if env_of(k): continue
        if k in owner or not h['land']:
            free = [(k[0] + dq, k[1] + dr) for dq, dr in NEIGHBOURS]
            free = [f for f in free if f in cells and cells[f]['land'] and f not in owner]
            if not free: continue
            k = min(free, key=lambda f: (cells[f]['lon'] - s['lon']) ** 2 + (cells[f]['lat'] - s['lat']) ** 2)
        owner[k] = s
    if recipes:
        # Then the 1948 localities, largest first, each on its own hex. A hex already taken keeps its owner: the
        # smaller place is left off, never pushed to a neighbour, so places stay where they were.
        pop = populations(os.path.join(ROOT, 'content/map/settlement_population_1945.yaml'))
        for s in owner.values(): s['population'] = pop.get(s['id'], 0)
        least = recipes.defaults.get('min_population', 0)
        for s in sorted(localities(os.path.join(ROOT, 'content/map/localities_1948.tsv')), key=lambda s: (-s['population'], s['id'])):
            if s['population'] < least or not in_box(s): continue
            h = nearest(s['lon'], s['lat']); k = (h['q'], h['r'])
            if k in owner or not h['land'] or env_of(k): continue
            owner[k] = s

    out, used_regions = [], set()
    lowland = recipes.defaults.get('lowland', LOWLAND) if recipes else LOWLAND
    for k, h in sorted(cells.items(), key=lambda kv: (kv[0][1], kv[0][0])):
        terrain = terrain_of(h)
        e = {'q': k[0], 'r': k[1], 'terrain': terrain, 'ground': 'seabed' if terrain == 'water' else GROUND.get(h['biome'], 'grass'),
             'prototype': '', 'note': '', 'unit': '', 'facing': 0.0, 'env': env_of(k)}
        region = None
        if recipes:
            region = recipes.region_of(h)
            e['region'] = region
            if terrain != 'water':
                e['ground'] = recipes.ground(region, h['biome'])
                used_regions.add(region)
        if e['env']:
            # Environment: terrain and ground only; the builder dresses it as continuous landscape (no tiles, no hex lines).
            if terrain == 'mountain': e['terrain'] = 'hills'
            out.append(e); continue
        if terrain != 'water':
            coastal = any((k[0] + dq, k[1] + dr) in by and not by[(k[0] + dq, k[1] + dr)]['land'] for dq, dr in NEIGHBOURS)
            ground = None
            if k in owner and recipes:
                s = owner[k]
                e['prototype'], ground = apply_rules(recipes.rules(region, 'settlements'), k[0], k[1], id=s['id'], type=s.get('type'),
                                                     tier=recipes.tier_of(s.get('population')), terrain=terrain, biome=h['biome'])
                e['note'] = s['id']
            elif k in owner:
                s = owner[k]
                e['prototype'] = SETTLEMENT_TILE.get(s['id'], TYPE_TILE.get(s.get('type'), 'moshava_village'))
                e['note'] = s['id']
                if e['prototype'] in ('tel_aviv_city', 'moshava_village', 'kibbutz') and terrain == 'mountain': e['terrain'] = 'hills'
            elif recipes:
                e['prototype'], ground, set_terrain = apply_rule(recipes.rules(region, 'nature'), k[0], k[1], terrain=terrain, biome=h['biome'], coastal=coastal)
                if set_terrain: e['terrain'] = set_terrain
            else:
                e['prototype'] = nature_tile(h, terrain, coastal)
                if e['prototype'] == 'coastal_dunes': e['ground'] = 'beach'
            if ground: e['ground'] = ground
            if e['prototype'] in lowland and terrain == 'mountain':
                e['terrain'] = 'hills'
        out.append(e)

    hex_of = {s['id']: k for k, s in owner.items()}
    if recipes and 'roads' in recipes.defaults:
        # Roads: the traced 1948 main roads, hex centre to hex centre, cut where they cross the environment.
        rd = recipes.defaults['roads']
        roads = []
        for road in route_roads(cells, rd.get('kinds', ['road']), str(rd.get('before', '1948-05-15'))):
            hs, run = road['hexes'], []
            for i in range(0, len(hs), 2):
                if not env_of((hs[i], hs[i + 1])): run += [hs[i], hs[i + 1]]; continue
                if len(run) >= 4: roads.append({'note': road['note'], 'hexes': run})
                run = []
            if len(run) >= 4: roads.append({'note': road['note'], 'hexes': run})
    else:
        # Roads: Civ-style, hex centre to hex centre, between the main towns (drawn along hex lines).
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
    units = [(u['unit'], u['near'], float(u['facing'])) for u in recipes.defaults.get('units', [])] if recipes else \
        [('palmach_squad', 'tel_aviv', 40.0), ('sandwich_truck', 'ramla', 100.0), ('willys_jeep', 'jerusalem', 270.0)]
    for u in units: stand(*u)

    board = {'id': a.id, 'title': f'{a.id} (generated by tools/hex_tiles/make_board.py; lat {a.lat}, lon {a.lon})', 'hexes': out, 'roads': roads, 'rivers': rivers}
    if recipes:
        board['title'] = f'{a.id} (generated by tools/hex_tiles/make_board.py from {os.path.basename(a.regions)}; lat {a.lat}, lon {a.lon})'
        board['regions'] = [flora_entry(recipes.by_id[r]) for r in recipes.order if r in used_regions]
    return board, owner


def main(argv=None):
    a = parse(argv)
    board, owner = build_board(a)
    dst = a.out or os.path.join(BOARDS, a.id + '.json')
    ov = overrides_path(dst)
    if os.path.exists(ov):
        with open(ov, encoding='utf-8') as f: missing = apply_overrides(board, json.load(f))
        for field, ed in missing: print(f' {field} override of ({ed["hex"]["q"]},{ed["hex"]["r"]}): no such hex on this board, not applied')
    with open(dst, 'w') as f: json.dump(board, f, indent=1)
    from collections import Counter
    out, roads = board['hexes'], board['roads']
    print(f'{dst}: {len(out)} hexes ({sum(1 for e in out if e["terrain"] != "water")} land), {len(roads)} roads, settlements {len(owner)}')
    print(' terrain', dict(Counter(e['terrain'] for e in out)))
    print(' tiles', dict(Counter(e['prototype'] for e in out if e['prototype'])))
    if 'regions' in board: print(' regions', dict(Counter(e['region'] for e in out if e['terrain'] != 'water')))


if __name__ == '__main__':
    sys.exit(main())
