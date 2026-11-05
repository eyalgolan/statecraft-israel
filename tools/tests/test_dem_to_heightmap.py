import json

import numpy as np
import pytest
import rasterio
from rasterio.transform import from_origin

import dem_to_heightmap as dem
import heightmap_codec as hc


def make_tile(path, lat, lon, value, size=60):
    """1x1 degree synthetic GeoTIFF whose top-left corner is (lon, lat + 1)."""
    data = np.full((size, size), value, dtype="float32")
    with rasterio.open(path, "w", driver="GTiff", height=size, width=size, count=1, dtype="float32",
                       crs="EPSG:4326", transform=from_origin(lon, lat + 1, 1 / size, 1 / size)) as ds:
        ds.write(data, 1)


def test_tile_url_matches_copernicus_naming():
    assert dem.tile_url(31, 34) == (
        "https://copernicus-dem-30m.s3.amazonaws.com/Copernicus_DSM_COG_10_N31_00_E034_00_DEM/"
        "Copernicus_DSM_COG_10_N31_00_E034_00_DEM.tif")


def test_tiles_for_bounds_covers_every_degree_cell():
    assert dem.tiles_for_bounds(dem.BOUNDS) == [(lat, lon) for lat in range(29, 34) for lon in (34, 35)]


def test_missing_tile_is_sea_and_mosaic_keeps_north_up(tmp_path):
    make_tile(tmp_path / "n31e034.tif", 31, 34, 100.0)   # southern tile
    make_tile(tmp_path / "n32e034.tif", 32, 34, -50.0)   # northern tile
    bounds = {"lon0": 34.0, "lon1": 35.0, "lat0": 30.0, "lat1": 33.0}  # lat 30-31 has no tile -> sea
    elev = dem.mosaic([tmp_path / "n31e034.tif", tmp_path / "n32e034.tif"], bounds, 30)
    assert elev.shape == (30, 30)
    assert elev[0, 15] == pytest.approx(-50.0)   # row 0 = north edge (lat 33)
    assert elev[15, 15] == pytest.approx(100.0)  # middle third = lat 31-32
    assert elev[-1, 15] == 0.0                    # no tile: sea level


def test_write_heightmap_normalises_and_writes_metadata(tmp_path):
    elev = np.array([[-430.0, 0.0], [570.0, 1000.0]], dtype="float32")
    meta = dem.write_heightmap(elev, dem.BOUNDS, tmp_path)
    assert not (tmp_path / "heightmap.r16").exists(), "only the packed heightmap is written (issue #44)"
    raw = hc.read(tmp_path, {"width": 2, "height": 2})
    assert raw[0, 0] == 0 and raw[1, 1] == 65535
    assert raw[1, 0] == round((570 + 430) / 1430 * 65535)
    on_disk = json.loads((tmp_path / "heightmap.json").read_text())
    assert on_disk == meta
    assert meta["elev_min_m"] == -430.0 and meta["elev_max_m"] == 1000.0
    assert meta["width"] == 2 and meta["height"] == 2
    assert meta["width_km"] == pytest.approx(1.7 * 111.32 * np.cos(np.radians(31.4)), rel=1e-6)
    assert meta["length_km"] == pytest.approx(3.9 * 110.57, rel=1e-6)
    assert "Copernicus" in meta["attribution"]


def test_main_refuses_when_every_tile_is_missing(monkeypatch):
    """#2652: every tile answering 404 (a wrong endpoint, say) is refused, not written as an all-sea map."""
    monkeypatch.setattr(dem, "tiles_for_bounds", lambda _: [(29, 34), (29, 35)])
    monkeypatch.setattr(dem, "fetch_tile", lambda *_: None)
    monkeypatch.setattr(dem, "mosaic", lambda *_: pytest.fail("mosaic must not run without tile data"))
    with pytest.raises(RuntimeError, match="no DEM tile has data"):
        dem.main()


def test_write_heightmap_refuses_a_flat_grid_before_writing(tmp_path):
    out = tmp_path / "map"
    with pytest.raises(ValueError, match="the DEM is flat"):
        dem.write_heightmap(np.zeros((2, 2), dtype="float32"), dem.BOUNDS, out)
    assert not out.exists()
