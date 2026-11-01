#nullable enable
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Desktop Quit (#2153): a Quit button on the start screen and in Settings for the desktop players (Steam, the
/// GitHub archives), hidden in the browser, where a page cannot close itself.</summary>
public class QuitButtonTests
{
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");

    [TearDown] public void PutTheDefaultsBack() { QuitButton.Platform = QuitButton.DefaultPlatform; QuitButton.Quitter = QuitButton.DefaultQuit; }

    [TestCase(RuntimePlatform.WebGLPlayer, false)]
    [TestCase(RuntimePlatform.WindowsPlayer, true)]
    [TestCase(RuntimePlatform.OSXPlayer, true)]
    [TestCase(RuntimePlatform.LinuxPlayer, true)]
    [TestCase(RuntimePlatform.WindowsEditor, true)]
    [TestCase(RuntimePlatform.OSXEditor, true)]
    [TestCase(RuntimePlatform.LinuxEditor, true)]
    public void It_shows_on_desktop_and_never_in_the_browser(RuntimePlatform p, bool shown)
    {
        Assert.AreEqual(shown, QuitButton.Shown(p));
    }

    [TestCase("start-screen", "start-quit")]
    [TestCase("screen-settings", "settings-quit")]
    public void Each_screen_has_exactly_one_quit_button(string screen, string name)
    {
        using var ui = new UiTestKit();
        Assert.AreEqual(1, ui.Root.Query<Button>(name).ToList().Count, name);
        Assert.AreEqual(1, ui.Root.Q(screen).Query<Button>(name).ToList().Count, $"{name} is on {screen}");
    }

    [TestCase("start-quit")]
    [TestCase("settings-quit")]
    public void In_the_browser_it_is_hidden(string name)
    {
        using var ui = new UiTestKit();
        int quits = 0;
        var unbind = QuitButton.Bind(ui.Q<Button>(name), RuntimePlatform.WebGLPlayer, () => quits++);
        Assert.AreEqual(DisplayStyle.None, ui.Q<Button>(name).style.display.value);
        ui.Press(name);
        Assert.AreEqual(0, quits, "nothing is bound in the browser");
        unbind();
    }

    /// <summary>Settings hides Quit's whole row in the browser, so no empty row is left at the bottom of the screen.</summary>
    [Test]
    public void In_the_browser_settings_hides_the_whole_row()
    {
        using var ui = new UiTestKit();
        var b = ui.Q<Button>("settings-quit");
        QuitButton.Bind(b, RuntimePlatform.WebGLPlayer, () => { }, ui.Q<VisualElement>("settings-quit-row"))();
        Assert.AreSame(ui.Q<VisualElement>("settings-quit-row"), b.parent, "the row is the button's own");
        Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("settings-quit-row").style.display.value);
        QuitButton.Bind(b, RuntimePlatform.LinuxPlayer, () => { }, ui.Q<VisualElement>("settings-quit-row"))();
        Assert.AreEqual(StyleKeyword.Null, ui.Q<VisualElement>("settings-quit-row").style.display.keyword);
    }

    [TestCase("start-quit")]
    [TestCase("settings-quit")]
    public void On_desktop_it_shows_and_a_press_quits_once(string name)
    {
        using var ui = new UiTestKit();
        int quits = 0;
        ui.Q<Button>(name).style.display = DisplayStyle.None;
        var unbind = QuitButton.Bind(ui.Q<Button>(name), RuntimePlatform.WindowsPlayer, () => quits++);
        Assert.AreEqual(StyleKeyword.Null, ui.Q<Button>(name).style.display.keyword, "shown: its inline display is cleared, so the stylesheet decides");
        ui.Press(name);
        Assert.AreEqual(1, quits);
        unbind();
        ui.Press(name);
        Assert.AreEqual(1, quits, "unbound: a rebuilt screen (the language switch) never quits twice");
    }

    [Test]
    public void Both_languages_name_it()
    {
        Assert.AreEqual("Quit", En["ui.quit"]);
        Assert.AreEqual("יציאה", He["ui.quit"]);
    }

    /// <summary>The presenters bind it in the UI's language (the editor is a desktop platform, so it shows here).</summary>
    [Test]
    public void Settings_shows_it_in_the_UIs_language()
    {
        foreach (var text in new[] { En, He })
        {
            using var ui = new UiTestKit();
            ui.Q<Button>("settings-quit").text = "";   // so the English, which Game.uxml also writes, proves the binding too
            using (new SettingsPresenter(ui.Root, text, null))
            {
                Assert.AreEqual(text["ui.quit"], ui.Q<Button>("settings-quit").text);
            }
        }
    }

    [Test]
    public void The_start_screen_shows_it_in_the_UIs_language()
    {
        var c = ContentLoader.Load(Paths.Content);
        foreach (var text in new[] { En, He })
        {
            using var ui = new UiTestKit();
            var store = new SaveStore(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statecraft-quit-" + System.Guid.NewGuid().ToString("N")));
            var bridge = new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), text);
            using var toast = new ToastPresenter(ui.Root, bridge, text);
            using var newGame = new NewGamePresenter(ui.Root, bridge, store, toast, text, () => 5UL);
            ui.Q<Button>("start-quit").text = "";   // so the English, which Game.uxml also writes, proves the binding too
            using (new StartScreenPresenter(ui.Root, newGame, false, text: text))
            {
                Assert.AreEqual(text["ui.quit"], ui.Q<Button>("start-quit").text);
            }
        }
    }

    // Review round (k2 C1): the presenters' own binding, through QuitButton.Platform and QuitButton.Quitter.

    static NewGamePresenter NewGame(UiTestKit ui, out ToastPresenter toast)
    {
        var c = ContentLoader.Load(Paths.Content);
        var store = new SaveStore(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "statecraft-quit-" + System.Guid.NewGuid().ToString("N")));
        var bridge = new SimBridge(GameSession.New(c, GoldenRun.Seed, Mode.Historical), En);
        toast = new ToastPresenter(ui.Root, bridge, En);
        return new NewGamePresenter(ui.Root, bridge, store, toast, En, () => 5UL);
    }

    [Test]
    public void A_screen_built_again_quits_once()
    {
        int quits = 0;
        QuitButton.Platform = () => RuntimePlatform.WindowsPlayer;
        QuitButton.Quitter = () => quits++;
        using (var ui = new UiTestKit())
        {
            using var newGame = NewGame(ui, out var toast);
            using (toast)
            {
                new StartScreenPresenter(ui.Root, newGame, false).Dispose();   // the language switch disposes the old screen
                using (new StartScreenPresenter(ui.Root, newGame, false)) ui.Press("start-quit");
            }
        }
        Assert.AreEqual(1, quits, "start screen");
        quits = 0;
        using (var ui = new UiTestKit())
        {
            new SettingsPresenter(ui.Root, En, null).Dispose();
            using (new SettingsPresenter(ui.Root, En, null)) ui.Press("settings-quit");
        }
        Assert.AreEqual(1, quits, "Settings");
    }

    [Test]
    public void In_the_browser_the_presenters_hide_it()
    {
        QuitButton.Platform = () => RuntimePlatform.WebGLPlayer;
        using (var ui = new UiTestKit())
        {
            using var newGame = NewGame(ui, out var toast);
            using (toast)
            using (new StartScreenPresenter(ui.Root, newGame, false))
                Assert.AreEqual(DisplayStyle.None, ui.Q<Button>("start-quit").style.display.value);
        }
        using (var ui = new UiTestKit())
        using (new SettingsPresenter(ui.Root, En, null))   // its Show() runs after the binding: the row stays hidden
            Assert.AreEqual(DisplayStyle.None, ui.Q<VisualElement>("settings-quit-row").style.display.value, "Settings hides Quit's whole row");
    }
}
