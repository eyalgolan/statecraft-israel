#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The advisors' encyclopedia (spec 10), which replaces How to play: one page per mechanic from
    /// advisor-messages.yaml, grouped by advisor, each with its title, its text and a Show me when it names a target. A
    /// normal side-menu screen, so ScreenNav owns opening and closing. In Hebrew a page waiting for its native review is
    /// left out (OQ-4), and an advisor with no page shown has no heading.</summary>
    public sealed class EncyclopediaPresenter : System.IDisposable
    {
        readonly VisualElement screen, pagePane;
        readonly ScrollView list;
        readonly Label pageTitle, pageText;
        readonly Button back, showMe, opening;
        readonly UiText text; readonly AdvisorCatalog catalog; readonly ScreenNav nav; readonly AdvisorPresenter? advisor;
        readonly bool hebrew, gdd03;
        readonly System.Action onBack, onShowMe, onOpening;

        /// <summary>The page on show, or null while the list shows.</summary>
        public string? ShowingPage { get; private set; }

        public EncyclopediaPresenter(VisualElement root, UiText text, AdvisorCatalog catalog, ScreenNav nav, AdvisorPresenter? advisor,
            string ruleset = SaveGame.GddRuleset)
        {
            this.text = text; this.catalog = catalog; this.nav = nav; this.advisor = advisor;
            hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            gdd03 = ruleset == SaveGame.Gdd03Ruleset;   // R-GOV-002: gdd-0.3 has no Attention, so its help never names it (#1688)
            screen = root.Q<VisualElement>("screen-encyclopedia");
            screen.Q<Label>("encyclopedia-title").text = text["encyclopedia.title"];
            list = screen.Q<ScrollView>("encyclopedia-list");
            pagePane = screen.Q<VisualElement>("encyclopedia-page");
            pageTitle = screen.Q<Label>("encyclopedia-page-title"); pageText = screen.Q<Label>("encyclopedia-page-text");
            back = screen.Q<Button>("encyclopedia-back"); showMe = screen.Q<Button>("encyclopedia-show-me");
            opening = screen.Q<Button>("encyclopedia-opening");
            back.text = text["encyclopedia.back"]; showMe.text = text["advisor.show_me"]; opening.text = text["encyclopedia.opening"];
            onBack = Render;
            onShowMe = () =>
            {
                var target = ShowingPage == null ? "" : catalog.FindPage(ShowingPage)?.ShowMe ?? "";
                if (target == "" || advisor == null) return;
                nav.Close();
                advisor.ShowTarget(target);
            };
            // The advisors' opening chain again (spec 9), which closes this screen. No button that opens nothing: while
            // every opening line is pending in Hebrew the chain cannot open (OQ-4).
            onOpening = () => { nav.Close(); advisor?.OpenChain(); };
            back.clicked += onBack; showMe.clicked += onShowMe; opening.clicked += onOpening;
            Display(opening, advisor != null && advisor.Chain.Any(m => m.ApprovedIn(hebrew)));
            ScreenNav.OnOpened(screen, Render);   // each open starts at the list
            Render();
        }

        /// <summary>A page the player may read here: approved in this language, and none about Attention under gdd-0.3.</summary>
        bool Shown(AdvisorCatalog.Page p) => p.ApprovedIn(hebrew) && !(gdd03 && p.Id == "attention");

        /// <summary>The list: per advisor, in AdvisorCatalog.AdvisorIds' order, a heading and one button per page shown.</summary>
        public void Render()
        {
            ShowingPage = null;
            list.Clear();
            foreach (var id in AdvisorCatalog.AdvisorIds)
            {
                var pages = catalog.Pages.Where(p => p.Advisor == id && Shown(p)).ToList();
                if (pages.Count == 0) continue;
                var heading = new Label(text["advisor." + id + ".name"]) { name = "encyclopedia-advisor-" + id };
                heading.AddToClassList("encyclopedia-advisor");
                list.Add(heading);
                foreach (var p in pages)
                {
                    var page = p.Id;
                    var button = new Button(() => ShowPage(page)) { name = "encyclopedia-page-" + page, text = text["advisor.page." + page + ".title"] };
                    button.AddToClassList("encyclopedia-entry");
                    list.Add(button);
                }
            }
            Display(list, true); Display(pagePane, false);
        }

        /// <summary>Opens the screen on <paramref name="pageId"/> (an advisor's Tell me more); on the list when that page is not
        /// shown in this language or ruleset.</summary>
        public void Open(string pageId)
        {
            if (nav.Current != "encyclopedia") nav.Open("encyclopedia");
            else Render();
            ShowPage(pageId);
        }

        void ShowPage(string pageId)
        {
            if (catalog.FindPage(pageId) is not AdvisorCatalog.Page p || !Shown(p)) return;
            ShowingPage = p.Id;
            var key = "advisor.page." + p.Id;
            pageTitle.text = text[key + ".title"];
            pageText.text = text[gdd03 && text.Has(key + ".gdd03.text") ? key + ".gdd03.text" : key + ".text"];
            Display(showMe, advisor != null && p.ShowMe != "" && !(gdd03 && p.ShowMe == "attention"));
            Display(list, false); Display(pagePane, true);
        }

        static void Display(VisualElement e, bool shown) => e.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;

        public void Dispose()
        {
            back.clicked -= onBack; showMe.clicked -= onShowMe; opening.clicked -= onOpening;
        }
    }
}
