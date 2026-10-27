#nullable enable
using System;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Read-only pioneer facts. Identity, work and location always come from the displayed sim state.</summary>
    public static class PioneerCard
    {
        public static (string Title, string Track, string Location, string Status) For(
            Content c, GameState s, UiText text, int id, Func<string, string> townName)
        {
            var u = s.Units[id];
            string SiteName(Statecraft.Sim.Site site) => text["ui.lang"] == UiLanguage.Hebrew && !string.IsNullOrWhiteSpace(site.NameHe) ? site.NameHe : site.Name;
            var hex = c.HexAt(u.Q, u.R);
            var places = c.SettlementHexes.Where(x => x.Q == u.Q && x.R == u.R).Select(x => townName(x.Id))
                .Concat(c.Sites.Where(x => x.Q == u.Q && x.R == u.R).Select(SiteName)).Distinct();
            // Same player-facing hex convention as InfoPanelPresenter: terrain and elevation, never axial coordinates.
            var terrain = hex == null ? text["info.off_map"] : text.Format("info.hex", text["info.biome." + hex.Biome], hex.ElevM);
            var location = text.Format("pioneer.card.location", string.Join(" · ", places.Concat(new[] { terrain })));
            var status = text["pioneer.card.idle"];
            var siteWork = s.Sites.Values.FirstOrDefault(w => w.BuiltOn == null && w.Unit == id);
            if (siteWork != null)
            {
                var site = c.Sites.First(x => x.Id == siteWork.Site);
                status = text.Format("pioneer.card.working", SiteName(site));
            }
            else if (Corridors.WorkOf(s, id) is var (work, index))
            {
                var d = c.Corridors.First(x => x.Id == work.Id);
                var name = text["ui.lang"] == UiLanguage.Hebrew && !string.IsNullOrWhiteSpace(d.NameHe) ? d.NameHe : d.Name;
                status = text.Format("pioneer.card.working", text.Format("corridor.segment", name, index + 1));
            }
            return (GddHexPanel.Group(text, s, id),
                text.Format("pioneer.card.track", text[u.Type == Projects.VeteranType ? "unit.name.veteran_halutz" : "city.track_halutz"]), location, status);
        }
    }
}
