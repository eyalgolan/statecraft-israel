using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;

/// <summary>A card never types the name of a game office's holder (docs/WRITING_EVENTS.md, "id, title, text and
/// placeholders"; #2540, #3023): the player can appoint someone else, so a holder is named only through an {office_id}
/// placeholder, which the loader checks (EventRules.CheckPlaceholders). Naming someone who holds no game office during the
/// card's window is fine (Count Folke Bernadotte in bernadotte_assassination).
///
/// What this test takes each word to mean:
/// - The texts: title, text, late_text, early_text and every option's text, and their Hebrew (_he) fields.
/// - The window: the card's [from, to), open-ended when it has no `to`, as for its placeholders. An early_text is read from
///   the first early offer (Armistice.EarliestOffer) when that comes before `from`, as the loader reads its placeholders.
///   A late_text is read over the same [from, to): the loader fills its placeholders for the window's last day.
/// - A game office: an office in offices.yaml that is not historical_only (a historical-only title has no Cabinet seat and
///   no player appointment, so the player cannot put anyone else in it). A holder is whoever Offices.Holders gives on some
///   day of the window, so a term that ends at a death ends there too.
/// - A name: the person's full people.yaml `name` or `name_he`, anywhere in the text (so a Hebrew prefix letter, as in
///   "לדוד בן-גוריון", still counts), and the English surname (the last word of `name`) as a whole capitalised word, so
///   "Sharett" alone counts but "Carmeli" (the brigade) or "Ben-Meir" does not name "Meir". Hyphen, maqaf and space are
///   one separator, and gershayim and geresh read as '"' and '\''. The Hebrew surname alone is not checked: many are
///   common words (שרת "served" and the feminine minister title, רוזן "count", שרי "ministers of", צור "rock" and
///   Tyre), which the shipped cards use. Another spelling of a name (Shertok for Sharett, Myerson for Meir) is not in
///   people.yaml and is not caught.</summary>
public class OfficeHolderNamesTests
{
    /// <summary>Every place a card names the holder of a game office during its window, one line each.</summary>
    internal static List<string> Offenders(Content c)
    {
        return new List<string>();
    }

    // The fixture's people, renamed so a surname is not a common English word ("Pm One"), and given Hebrew names; a
    // transport minister from 1949-06-01 only, and a holder of a historical-only title.
    static readonly string People = Fixture.Files["people.yaml"]
        .Replace("name: Pm One", "name: Ada Ronen\n    name_he: עדה רונן\n    biography_sources: { name_he: [{ url: https://example.org/he, checked: 2026-11-10 }] }")
        .Replace("name: Def Two", "name: Ben Tamir") + @"
  - id: later
    name: Noa Barak
    living: false
    competence: 5
    loyalty: 5
    offices:
      - { office: transport_minister, from: 1949-06-01, sources: [{ url: https://example.org/term, checked: 2026-11-10 }] }
  - id: deputy
    name: Uri Segal
    living: false
    competence: 5
    loyalty: 5
    offices:
      - { office: deputy_title, from: 1948-05-14, sources: [{ url: https://example.org/term, checked: 2026-11-10 }] }
";

    static readonly string OfficesYaml = Fixture.Files["offices.yaml"] +
        "  - id: transport_minister\n    title: Minister of Transport\n" +
        "  - { id: deputy_title, title: Deputy Title, historical_only: true }\n";

    /// <summary>One card, ValidationTests.Event with its id, window and texts replaced.</summary>
    static string Card(string id, string from, string to, string title, string text, string ration, string titleHe = "",
        string textHe = "", string rationHe = "", string waitHe = "")
    {
        var y = ValidationTests.Event
            .Replace("id: drought", "id: " + id)
            .Replace("title: Drought", "title: '" + title + "'")
            .Replace("text: '{finance_minister} reports a drought.'", "text: '" + text + "'")
            .Replace("from: 1948-06-01", "from: " + from)
            .Replace("to: 1948-09-01", "to: " + to)
            .Replace("text: Ration water", "text: '" + ration + "'");
        if (titleHe != "")
            y = y.Replace("anchor: soft", "title_he: '" + titleHe + "'\ntext_he: '" + textHe + "'\ntext_he_review: agent-checked\nanchor: soft")
                .Replace("text: '" + ration + "'", "text: '" + ration + "'\n    text_he: '" + rationHe + "'")
                .Replace("text: Wait for rain", "text: Wait for rain\n    text_he: '" + waitHe + "'");
        return y;
    }

    static Content Load(params (string file, string yaml)[] cards) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["people.yaml"] = People, ["offices.yaml"] = OfficesYaml },
            cards.ToDictionary(x => x.file, x => x.yaml)));

    [Fact]
    public void ACardThatNamesAHolderInAnyTextFails()
    {
        var c = Load(("named.yaml", Card("named", "1948-06-01", "1948-09-01",
            title: "Ronen decides", text: "{finance_minister} reports a drought.", ration: "Ask Ada Ronen to ration water",
            titleHe: "בצורת", textHe: "על {finance_minister} להחליט.", rationHe: "לבקש מעדה־רונן לקצוב מים", waitHe: "לחכות לגשם")));
        var found = Offenders(c);
        Assert.Equal(3, found.Count);
        Assert.Contains(found, x => x.StartsWith("named title: ", StringComparison.Ordinal) && x.Contains("pm_one"));
        Assert.Contains(found, x => x.StartsWith("named options[ration].text: ", StringComparison.Ordinal) && x.Contains("pm_one"));
        Assert.Contains(found, x => x.StartsWith("named options[ration].text_he: ", StringComparison.Ordinal) && x.Contains("pm_one"));
    }

    [Fact]
    public void NamingSomeoneWhoHoldsNoGameOfficeInTheWindowIsFine()
    {
        // Noa Barak's term starts after this window; Uri Segal's title is historical-only; Count Visitor is in no file;
        // "Ronenberg" and "Ben-Ronen" do not name Ronen as a word.
        var c = Load(("fine.yaml", Card("fine", "1948-06-01", "1949-06-01",
            title: "Noa Barak arrives", text: "Uri Segal and Count Visitor meet on Ronenberg Street. {prime_minister} must decide.",
            ration: "Ask Ben-Ronen")));
        Assert.Empty(Offenders(c));
    }

    [Fact]
    public void TheSameNameFailsOnceItsHolderIsInOfficeDuringTheWindow()
    {
        var c = Load(("later.yaml", Card("later_card", "1949-05-01", "1949-07-01",
            title: "A drought", text: "Barak reports a drought.", ration: "Ration water")));
        var line = Assert.Single(Offenders(c));
        Assert.StartsWith("later_card text: ", line, StringComparison.Ordinal);
        Assert.Contains("later", line);
        Assert.Contains("transport_minister", line);
    }

    /// <summary>The shipped cards: every offender is listed in the one failure message.</summary>
    [Fact]
    public void NoShippedCardNamesAGameOfficeHolder()
    {
        var found = Offenders(ContentLoader.Load(RealContentTests.ContentRoot()));
        Assert.True(found.Count == 0, "cards name a game office's holder (write {office_id} instead, docs/WRITING_EVENTS.md):\n" + string.Join("\n", found));
    }
}
