#nullable enable
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

// Review-only capture seam: opt-in environment path, no game behaviour or test expectations depend on screenshots.
public class FoodArmsCaptureTests
{
    [UnityTest]
    public IEnumerator SameOpeningStateHudDefenceReportAndWhyFrames()
    {
        var dir=Environment.GetEnvironmentVariable("FOOD_ARMS_CAPTURE_DIR");
        if(!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        foreach(bool he in new[]{false,true})
        foreach(var size in new[]{(1920,1080),(1280,720),(375,667)})
        {
            var rt=new RenderTexture(size.Item1,size.Item2,0); rt.Create();
            var ps=Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture=rt; ps.clearColor = true; ps.colorClearValue = Color.black;
            var go=new GameObject("FoodArmsMatchedCapture"); go.SetActive(false);
            var doc=go.AddComponent<UIDocument>(); doc.panelSettings=ps; doc.visualTreeAsset=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml); go.SetActive(true);
            try
            {
                var root=doc.rootVisualElement;
                if(he) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
                root.Q("root").RemoveFromClassList("loading");
                var t=UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(he?UiPaths.StringsHebrew:UiPaths.Strings).text);
                var b=new SimBridge(GameSession.New(ContentLoader.Load(Paths.Content),GoldenRun.Seed,Mode.Historical),t);
                using var hud=new HudPresenter(root,b,t); using var nav=new ScreenNav(root); using var defence=new DefencePresenter(root,b,t);
                for(int i=0;i<25;i++)yield return null;
                Assert.Greater(root.Q("top-bar").worldBound.width,0);
                var unchanged=b.State.Hash();
                Save("hud");
                nav.Open("defence"); for(int i=0;i<20;i++)yield return null;
                root.Q<ScrollView>("defence-body").ScrollTo(root.Q("defence-budget"));
                for(int i=0;i<20;i++)yield return null;
                Assert.IsTrue(ScreenNav.IsOpen(root.Q("screen-defence")));
                Assert.AreEqual(unchanged,b.State.Hash(),"opening and scrolling Defence changes no sim state");
                Save("defence");
                // The panel after the budget share: Arms on this branch, the prisoners panel on main.
                root.Q<ScrollView>("defence-body").ScrollTo(root.Q("defence-arms") ?? root.Q("defence-prisoners"));
                for(int i=0;i<20;i++)yield return null;
                Save("defence-lower");
                // The same turn on both trees: one End turn with no order, then the report it opens and Why.
                nav.Close(); for(int i=0;i<5;i++)yield return null;
                using var report=new TurnReportPresenter(root,b,t,nav); using var why=new WhyPresenter(root,b,t);
                b.EndTurn(); for(int i=0;i<25;i++)yield return null;
                Assert.IsTrue(report.IsOpen,"the turn report opens after End turn");
                Save("report");
                report.Close(); nav.Open("why"); for(int i=0;i<25;i++)yield return null;
                Assert.IsTrue(ScreenNav.IsOpen(root.Q("screen-why")));
                Save("why");
                void Save(string area)
                {
                    if(string.IsNullOrEmpty(dir)) return;
                    var old=RenderTexture.active; var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                    try {RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(dir,FormattableString.Invariant($"{(he?"he":"en")}-{size.Item1}x{size.Item2}-{area}.png")),tex.EncodeToPNG());}
                    finally{RenderTexture.active=old;Object.DestroyImmediate(tex);}
                }
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(ps);rt.Release();Object.DestroyImmediate(rt);}
        }
    }
}
