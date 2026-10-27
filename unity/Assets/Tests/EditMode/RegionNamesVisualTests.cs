#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class RegionNamesVisualTests
{
    // Real UXML/panel/presenters, same seed/hex/date/size, before and after catalogue admission.
    // This covers the existing subdistrict/armistice display path without control-period overlays;
    // it does not force those bands over the default game's control overlay or claim a WebGL playtest.
    [UnityTest]
    public IEnumerator SourcedRegionAndZoneTextFitsAtDesktopAndPhoneSizes()
    {
        var input = GameInputs.MapFromDisk(Paths.Content, Paths.StreamingMap);
        var sub = Subdistricts1948.Parse(input.RegionsYaml); var arm = Armistice1949.Parse(input.ArmisticeYaml);
        var content = ContentLoader.Load(Paths.Content);
        var places = SettlementPlacer.Parse(input.SettlementsYaml).ToDictionary(x => x.Id, x => x.Name);
        var names = MapRegionNames.Parse(input.RegionNamesYaml);
        var ramallah = sub.Regions.Single(x => x.Key == "ramallah");
        var rh = sub.Layer.Hexes.First(x => sub.Layer.RegionOf(x.q,x.r) == ramallah.Id);
        var zh = arm.HexesOf(arm.Zones.Single(x => x.Key == "egypt_el_auja_dmz")).First();
        foreach (var he in new[] {false,true}) foreach (var size in new[] {(w:1920,h:1080),(w:390,h:844)})
        foreach (var scenario in new[] {(key:"subdistrict",q:rh.q,r:rh.r,date:new DateTime(1948,5,15),expected:"נפת ראם־אללה"),
            (key:"armistice",q:zh.q,r:zh.r,date:new DateTime(1950,3,1),expected:"שטח הכפר אל־עוג'ה וסביבתו")})
        foreach (var after in new[] {false,true})
        {
            var t=UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he?UiPaths.StringsHebrew:UiPaths.Strings).text);
            var regions=new MapRegions(sub,arm,names:after?names:null);
            var session=GameSession.New(content,GoldenRun.Seed,Mode.Historical); session.State.Date=scenario.date;
            var rt=new RenderTexture(size.w,size.h,0); rt.Create();
            var ps=Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture=rt;
            var go=new GameObject("RegionNamesVisualTest"); go.SetActive(false);
            var doc=go.AddComponent<UIDocument>();doc.panelSettings=ps;doc.visualTreeAsset=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);go.SetActive(true);
            var root=doc.rootVisualElement;root.Q("root").RemoveFromClassList("loading");
            if(he)root.Q("root").AddToClassList(UiLanguage.HebrewClass);
            var bridge=new SimBridge(session,t); var hud=new HudPresenter(root,bridge,t);
            var info=new InfoPanelPresenter(root,bridge,t,places,regions:regions);
            try
            {
                root.Q<Label>("map-layer").text=regions.Label(scenario.date,t.Format,he);
                root.Q("map-layer").style.display=DisplayStyle.Flex;
                info.Show(scenario.q,scenario.r);
                for(int i=0;i<20 && float.IsNaN(root.Q("info-panel").worldBound.width);i++)yield return null;
                for(int i=0;i<6;i++)yield return null;
                var label=root.Q<Label>("info-region");
                Assert.IsNotEmpty(label.text);
                if(he&&after)StringAssert.Contains(scenario.expected,label.text);
                Assert.Greater(label.contentRect.width,0);
                var measured=label.MeasureTextSize(label.text,label.contentRect.width,VisualElement.MeasureMode.Exactly,0,VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(measured.y,label.contentRect.height+2,$"{scenario.key} {size} he={he} after={after}: text must wrap inside its actual region label");
                var output=Environment.GetEnvironmentVariable("REGION_NAMES_CAPTURE_DIR");
                if(!string.IsNullOrWhiteSpace(output))
                {
                    Directory.CreateDirectory(output);
                    var previous=RenderTexture.active;var shot=new Texture2D(size.w,size.h,TextureFormat.RGB24,false);
                    try{RenderTexture.active=rt;shot.ReadPixels(new Rect(0,0,size.w,size.h),0,0);shot.Apply();File.WriteAllBytes(Path.Combine(output,$"{scenario.key}-{(he?"he":"en")}-{size.w}-{(after?"after":"before")}.png"),shot.EncodeToPNG());}
                    finally{RenderTexture.active=previous;Object.DestroyImmediate(shot);}
                }
            }
            finally{info.Dispose();hud.Dispose();Object.DestroyImmediate(go);Object.DestroyImmediate(ps);rt.Release();Object.DestroyImmediate(rt);}
        }
    }
}
