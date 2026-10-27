#nullable enable
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEngine;

public class RegionNamesHebrewTests
{
    // Gazette1415, Hebrew supplement2,7June1945:493/498. Independent identity oracle, not Names.Pick.
    [TestCase("acre", "נפת עכו", "מחוז הגליל", false)]
    [TestCase("beisan", "נפת בית־שאן", "מחוז הגליל", false)]
    [TestCase("ramallah", "נפת ראם־אללה", "מחוז ירושלים", false)]
    [TestCase("acre", "נפת עכו", "מחוז הגליל", true)]
    [TestCase("beisan", "נפת בית־שאן", "מחוז הגליל", true)]
    [TestCase("ramallah", "נפת ראם־אללה", "מחוז ירושלים", true)]
    public void RegionIdentityIsSourcedAndEnglishStaysUnchanged(string id, string name, string district, bool he)
    {
        var c=Paths.Content;
        var m=new MapRegions(Subdistricts1948.Parse(File.ReadAllText(Path.Combine(c,"map",Subdistricts1948.FileName))),
            Armistice1949.Parse(File.ReadAllText(Path.Combine(c,"map",Armistice1949.FileName))),
            names: MapRegionNames.Parse(File.ReadAllText(Path.Combine(c,"map",MapRegionNames.FileName))));
        var r=m.Subdistricts.Regions.Single(r=>r.Key==id);
        var hex=m.Subdistricts.Layer.Hexes.First(h=>m.Subdistricts.Layer.RegionOf(h.q,h.r)==r.Id);
        var t=UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he?UiPaths.StringsHebrew:UiPaths.Strings).text);
        Assert.IsTrue(string.Equals(t.Format("info.region",he?name:r.Name,he?district:r.District), InfoPanelPresenter.RegionText(m,t,hex.q,hex.r,new DateTime(1948,5,15)), StringComparison.Ordinal));
    }
}
