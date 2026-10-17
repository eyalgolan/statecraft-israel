#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A person card in the promo's style (promo/final/04 and 08), shared by the Cabinet and Defence screens: the
    /// framed portrait (or, with none in the catalog, a plain plate with the initials: never an invented likeness), the
    /// name, the office, the party, and competence and loyalty as two bars with their values out of 10 (people.yaml). A
    /// vacant office shows its title and "(vacant)" only. The card's elements are made once and updated in place, so a
    /// focused card keeps its focus. Element names start with the given prefix (<c>{prefix}-holder</c>, <c>-office</c>,
    /// <c>-party</c>, <c>-portrait</c>, <c>-initials</c>, <c>-competence</c>, <c>-competence-value</c>, <c>-loyalty</c>,
    /// <c>-loyalty-value</c>).</summary>
    public static class PersonCard
    {
        public const string PortraitClass = "card-portrait", InitialsClass = "card-initials", NameClass = "card-name",
            OfficeClass = "card-office", PartyClass = "card-party", StatClass = "card-stat";
        /// <summary>The scale of competence and loyalty (people.yaml; ContentLoader refuses values outside 0-10).</summary>
        public const int Scale = 10;

        /// <summary>The person's initials (first letter of the first and last words of the name), for the plate shown when
        /// there is no portrait.</summary>
        public static string Initials(string name)
        {
            var words = name.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "";
            return words.Length == 1 ? words[0].Substring(0, 1) : words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1);
        }

        /// <summary>The plate's text in Hebrew (Beri, native speaker, 17 October 2026): the first name's initial and the
        /// surname, "י. ידין", since two Hebrew letters ("יי") read as a gershayim more than as a name.</summary>
        public static string InitialAndSurname(string name)
        {
            var words = name.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "";
            return words.Length == 1 ? words[0] : words[0].Substring(0, 1) + ". " + words[words.Length - 1];
        }

        /// <summary>Makes the card's elements inside <paramref name="card"/> (cleared first), empty.</summary>
        public static void Build(VisualElement card, string prefix, UiText text)
        {
            card.Clear();
            var plate = Element(prefix + "-plate", "card-plate");
            plate.Add(Element(prefix + "-portrait", PortraitClass));
            plate.Add(Text(prefix + "-initials", InitialsClass));
            // The selected seat's word, shown only under .seat-selected (Statecraft.uss): a mark that is not a colour
            // change, since the selected fill and rim differ from the rest by 1.2:1 and 2.4:1 (#41 M6).
            var selected = Text(prefix + "-selected", "card-selected");
            selected.text = text["card.selected"];
            plate.Add(selected);
            card.Add(plate);
            card.Add(Text(prefix + "-holder", NameClass));
            card.Add(Text(prefix + "-office", OfficeClass));
            card.Add(Text(prefix + "-party", PartyClass));
            card.Add(Stat(prefix + "-competence", text["card.competence"], "ui-bar--competence"));
            card.Add(Stat(prefix + "-loyalty", text["card.loyalty"], "ui-bar--loyalty"));
        }

        /// <summary>Fills <paramref name="card"/> in place, building it first if it has not been built with this prefix.
        /// <paramref name="office"/> is the line under the name; <paramref name="portraitSize"/> is the portrait box in
        /// logical pixels (Statecraft.uss), used to keep the face in view.</summary>
        public static void Fill(VisualElement card, string prefix, Content c, GameState s, Person? person, string office, bool appointed,
                                UiText text, PortraitCatalog? portraits, Vector2 portraitSize, bool partyLabel = false)
        {
            if (card.Q(prefix + "-plate") == null) Build(card, prefix, text);
            var portrait = card.Q(prefix + "-portrait");
            var entry = portraits?.Find(person?.Id);
            var tex = entry?.image;
            if (tex != null)
            {
                portrait.style.backgroundImage = new StyleBackground(tex);
                Frame(portrait, entry!.Value, portraitSize);
            }
            else portrait.style.backgroundImage = StyleKeyword.None;
            portrait.style.display = tex != null ? DisplayStyle.Flex : DisplayStyle.None;   // never an invented portrait
            var initials = card.Q<Label>(prefix + "-initials");
            var name = person == null ? "" : ContentNames.Of(text, person);
            initials.text = person == null || tex != null ? "" : text["ui.lang"] == UiLanguage.Hebrew && person.NameHe != "" ? InitialAndSurname(name) : Initials(name);
            initials.style.display = tex == null ? DisplayStyle.Flex : DisplayStyle.None;

            var holder = card.Q<Label>(prefix + "-holder");
            holder.text = person == null ? text["cabinet.vacant"] : appointed ? text.Format("card.appointed_name", name) : name;
            holder.EnableInClassList("appointed", appointed);
            card.Q<Label>(prefix + "-office").text = office;

            var party = card.Q<Label>(prefix + "-party");
            var shown = person != null ? DisplayStyle.Flex : DisplayStyle.None;
            party.style.display = shown;
            card.Q(prefix + "-competence-row").style.display = shown;
            card.Q(prefix + "-loyalty-row").style.display = shown;
            if (person == null) return;
            var partyId = Offices.PartyOf(c, person, s.Date);
            var partyName = partyId == "" ? text["cabinet.no_party"] : ContentNames.Of(text, c.Parties.First(x => x.Id == partyId));
            party.text = partyLabel ? text.Format("card.party", partyName) : partyName;
            SetStat(card, prefix + "-competence", person.Competence);
            SetStat(card, prefix + "-loyalty", person.Loyalty);
        }

        /// <summary>Keeps the face in view in a portrait box of <paramref name="size"/> logical pixels (PortraitFraming).</summary>
        public static void Frame(VisualElement portrait, PortraitCatalog.Entry entry, Vector2 size) =>
            portrait.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top,
                new Length(PortraitFraming.PositionYPercent(entry.image.width, entry.image.height, size.x, size.y, entry.focusY), LengthUnit.Percent));

        /// <summary>Empties a card (a disposed screen keeps no stale person or texture).</summary>
        public static void Clear(VisualElement card) => card.Clear();

        static void SetStat(VisualElement card, string name, int value)
        {
            card.Q<UiBar>(name).Value = (float)value / Scale;
            card.Q<Label>(name + "-value").text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        static VisualElement Element(string name, string className)
        {
            var e = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            e.AddToClassList(className);
            return e;
        }

        static Label Text(string name, string className)
        {
            var l = new Label { name = name, pickingMode = PickingMode.Ignore };
            l.AddToClassList(className);
            return l;
        }

        static VisualElement Stat(string name, string caption, string modifier)
        {
            var row = Element(name + "-row", StatClass);
            var label = new Label(caption) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("card-stat-name");
            row.Add(label);
            var bar = new UiBar { name = name };
            bar.AddToClassList(modifier);
            row.Add(bar);
            row.Add(Text(name + "-value", "card-stat-value"));
            return row;
        }
    }
}
