#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Game.Map
{
    /// <summary>What the region renderer draws on a date (owner rulings 2026-10-11): the Mandate sub-districts
    /// (<see cref="Subdistricts1948"/>) from 15 May 1948 as the base, and each front's 1949 armistice features from the date
    /// that front switches: the sub-district layer's per-front end (<see cref="Subdistricts1948.FrontReplaced"/>, its
    /// agreement's date), only for fronts whose line exists (<see cref="Switched"/>). From then the front's zones (the
    /// Demilitarized Zones) are drawn over the sub-districts in the secondary style, its lines are named in the info panel on
    /// the hexes they pass through (where a line is the Mandate frontier, as for Lebanon, the band already drawn is that
    /// line) and the map label names the front; the sub-district bands themselves stay as the base. A front without line data keeps the Mandate layer, and the map label says so; a low-accuracy front
    /// (Jordan) is labelled so. The layer as a whole leaves the map only on the sub-district layer's own end
    /// (<see cref="Subdistricts1948.ShownUntil"/>), null until the armistice lines enclose each side's area. Display only: reads no sim state but the date it is given.</summary>
    public sealed class MapRegions
    {
        /// <summary>Zones are region ids ZoneIdBase + zone id in the composed layer, apart from the sub-districts' ids.</summary>
        public const int ZoneIdBase = 1000;

        public readonly Subdistricts1948 Subdistricts;
        public readonly Armistice1949? Armistice;
        readonly Dictionary<string, RegionLayer> composed = new Dictionary<string, RegionLayer>();

        public MapRegions(Subdistricts1948 subdistricts, Armistice1949? armistice)
        {
            Subdistricts = subdistricts ?? throw new ArgumentNullException(nameof(subdistricts));
            Armistice = armistice;
        }

        /// <summary>Whether <paramref name="front"/>'s armistice line has replaced the Mandate layer there on
        /// <paramref name="date"/>: the sub-district layer's own per-front end (<see cref="Subdistricts1948.FrontReplaced"/>)
        /// when it lists the front, else the front's own date. A front without line data never switches.</summary>
        public bool Switched(Armistice1949.Front front, DateTime date) =>
            front.ShownFrom != null && (Subdistricts.Fronts.ContainsKey(front.Key) ? Subdistricts.FrontReplaced(front.Key, date) : front.InForce(date));

        /// <summary>An item (zone or line) is shown from its own date, once its front has switched.</summary>
        public bool InForce(Armistice1949.Item item, DateTime date) => item.InForce(date) && Switched(item.Front, date);

        /// <summary>The armistice zones drawn on <paramref name="date"/>, in file order.</summary>
        public IReadOnlyList<Armistice1949.Item> ZonesOn(DateTime date) =>
            Armistice == null ? Array.Empty<Armistice1949.Item>() : Armistice.Zones.Where(z => InForce(z, date)).ToList();

        /// <summary>The zone drawn on the hex on <paramref name="date"/>, or null.</summary>
        public Armistice1949.Item? ZoneAt(int q, int r, DateTime date)
        {
            var z = Armistice?.ZoneAt(q, r, DateTime.MaxValue);
            return z != null && InForce(z, date) ? z : null;
        }

        /// <summary>The armistice lines shown through the hex on <paramref name="date"/>.</summary>
        public IEnumerable<Armistice1949.Item> LinesAt(int q, int r, DateTime date) =>
            Armistice == null ? Enumerable.Empty<Armistice1949.Item>() : Armistice.LinesAt(q, r, DateTime.MaxValue).Where(l => InForce(l, date));

        /// <summary>The layer drawn on <paramref name="date"/>: null outside the sub-district layer's window; the
        /// sub-district layer itself while no armistice zone is in force; otherwise that layer with the zones in force drawn
        /// over it (one composed layer per set of zones, built once).</summary>
        public RegionLayer? LayerOn(DateTime date)
        {
            if (!Subdistricts.ShowsOn(date)) return null;
            var zones = ZonesOn(date);
            if (zones.Count == 0) return Subdistricts.Layer;
            var key = string.Join(",", zones.Select(z => z.Id));
            if (composed.TryGetValue(key, out var layer)) return layer;
            var hexes = Subdistricts.Layer.Hexes.ToDictionary(h => h, h => Subdistricts.Layer.RegionOf(h.q, h.r));
            var styles = hexes.Values.Distinct().ToDictionary(id => id, id => Subdistricts.Layer.StyleOf(id));
            foreach (var z in zones)
            {
                styles[ZoneIdBase + z.Id] = z.Style;
                foreach (var h in Armistice!.HexesOf(z)) hexes[h] = ZoneIdBase + z.Id;
            }
            return composed[key] = new RegionLayer(Subdistricts.Layer.Name + "+armistice_1949", hexes, styles);
        }

        bool? shownState; RegionLayer? drawn;
        /// <summary>Whether the last <see cref="Apply"/> drew a layer.</summary>
        public bool Shown => shownState == true;

        /// <summary>Draws the layer for <paramref name="date"/> (MapBorders.Use) when it differs from the one drawn; a call
        /// that changes nothing leaves the map alone. Off the window, the layer is taken off if it is still ours.</summary>
        public void Apply(DateTime date)
        {
            var layer = LayerOn(date);
            if (shownState != null && ReferenceEquals(layer, drawn)) return;
            shownState = layer != null;
            if (layer != null) MapBorders.Use(layer);
            else if (drawn != null && MapBorders.Current == drawn) MapBorders.Use(null);
            drawn = layer;
        }

        /// <summary>The fronts whose armistice line is shown on <paramref name="date"/>, and those that keep the Mandate
        /// layer for want of line data (only listed once some front's line is shown).</summary>
        public (IReadOnlyList<Armistice1949.Front> inForce, IReadOnlyList<Armistice1949.Front> noData) FrontsOn(DateTime date)
        {
            if (Armistice == null) return (Array.Empty<Armistice1949.Front>(), Array.Empty<Armistice1949.Front>());
            var on = Armistice.Fronts.Where(f => Switched(f, date)).ToList();
            var none = on.Count == 0 ? new List<Armistice1949.Front>() : Armistice.Fronts.Where(f => f.ShownFrom == null).ToList();
            return (on, none);
        }

        /// <summary>The map label: the sub-district layer's, and once a front's line is shown, those fronts and the fronts
        /// still without line data. <paramref name="withLines"/> formats {0} label, {1} armistice label, {2} front names;
        /// <paramref name="noData"/> formats {0} a front's name, appended for each front without data; <paramref name="low"/>
        /// formats {0} a low-accuracy front's name (Jordan, from a low-resolution scan); <paramref name="note"/> formats {0} a
        /// front's name and {1} the notes of its items shown (the 1950 modus vivendi).</summary>
        public string Label(DateTime date, string withLines, string noData, string low = "{0}", string note = "{0} ({1})")
        {
            var (on, none) = FrontsOn(date);
            if (on.Count == 0 || Armistice == null) return Subdistricts.Label;
            string Name(Armistice1949.Front f)
            {
                var n = f.LowAccuracy ? string.Format(CultureInfo.InvariantCulture, low, f.Name) : f.Name;
                var notes = Armistice.Zones.Concat(Armistice.Lines).Where(x => x.Front == f && InForce(x, date) && x.LabelNote != "")
                    .Select(x => x.LabelNote).Distinct().ToList();
                return notes.Count == 0 ? n : string.Format(CultureInfo.InvariantCulture, note, n, string.Join(", ", notes));
            }
            var text = string.Format(CultureInfo.InvariantCulture, withLines, Subdistricts.Label, Armistice.Label, string.Join(", ", on.Select(Name)));
            foreach (var f in none) text += string.Format(CultureInfo.InvariantCulture, noData, f.Name);
            return text;
        }

        /// <summary>The agreement's date as players read it ("24 February 1949").</summary>
        public static string DateText(DateTime d) => d.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
    }
}
