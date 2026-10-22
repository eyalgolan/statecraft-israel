- Cities and regional landscapes for the opt-in hex-tile map.
  - City and town tiles: Haifa (port, and its bay as the edge), Acre, Safed, Tiberias, Nazareth, Netanya, Hadera,
    Rehovot, Ramla, Lydda and Beersheba.
  - Jerusalem's new city with the Old City (an alternative for its hex) and a garden-quarter tile.
  - A Galilee kibbutz, a Negev kibbutz, and a ma'abara (a painter tile for 1950 on).
  - Landscape tiles: the Upper Galilee hills, the undrained Hula swamp, the Jezreel valley, the Carmel, the Sharon's
    kurkar and citrus, the Shephelah terraces, the Judean desert, the Dead Sea shore, the Negev loess, the Ramon
    highlands and the Arava's acacia and oases.
  - The `israel` board now has 18 landscape regions and is clipped to Mandatory Palestine on land, with a fringe of
    landscape round it. Which line bounds the play area is still a design call.
  - Presentation only and off by default (refs #18).

### Changed
- The opt-in `israel` hex board uses a 2,000-person locality threshold: 61 village tiles and 16 regional farm/forest
  variants, with room for the Upper Galilee hills and Carmel ridge. All 25 core settlements and the play-area boundary
  are preserved; the simulation and shipped map are unchanged (refs #1154).
