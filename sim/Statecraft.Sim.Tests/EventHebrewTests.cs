using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Hebrew text on event cards (#1064, owner decision on #55): title_he, text_he and each option's text_he beside
/// the English, all or none, with text_he_review: agent-checked until a native speaker has read them (#564).</summary>
public class EventHebrewTests
{
    const string Title = "title: Drought\n", Ration = "    text: Ration water\n", Wait = "    text: Wait for rain\n";
    const string TitleHe = "title_he: בצורת\ntext_he: '{finance_minister} מדווח על בצורת.'\ntext_he_review: agent-checked\n";

    static string Hebrew => ValidationTests.Event
        .Replace(Title, Title + TitleHe)
        .Replace(Ration, Ration + "    text_he: לקצוב מים\n")
        .Replace(Wait, Wait + "    text_he: לחכות לגשם\n");

    static Content Load(string ev) => ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["drought.yaml"] = ev }));

    static ContentException Bad(string ev) => Assert.Throws<ContentException>(() => Load(ev));

    [Fact]
    public void TheHebrewTextLoadsBesideTheEnglish()
    {
        var e = Load(Hebrew).Events.Single(x => x.Id == "drought");
        Assert.Equal(("בצורת", "{finance_minister} מדווח על בצורת."), (e.TitleHe, e.TextHe));
        Assert.Equal(new[] { "לקצוב מים", "לחכות לגשם" }, e.Options.Select(o => o.TextHe));
        Assert.Equal((HebrewReview.AgentChecked, true), (e.TextHeReview, e.TextHeReviewGiven));
        Assert.Equal("Drought", e.Title);
    }

    [Fact]
    public void AnEventWithoutHebrewLoadsWithEmptyFields()
    {
        var e = Load(ValidationTests.Event).Events.Single(x => x.Id == "drought");
        Assert.Equal(("", "", false), (e.TitleHe, e.TextHe, e.TextHeReviewGiven));
        Assert.All(e.Options, o => Assert.Equal("", o.TextHe));
    }

    [Theory]
    // All or none: a card half in Hebrew would mix the languages.
    [InlineData("title_he: בצורת\n", "", "events[drought].title_he", "required")]
    [InlineData("text_he: '{finance_minister} מדווח על בצורת.'\n", "", "events[drought].text_he", "required")]
    [InlineData("    text_he: לחכות לגשם\n", "", "events[drought].options[wait].text_he", "required")]
    [InlineData("title_he: בצורת\ntext_he: '{finance_minister} מדווח על בצורת.'\n", "", "events[drought].title_he", "required")]
    // In Hebrew letters, with placeholders that resolve.
    [InlineData("title_he: בצורת\n", "title_he: Drought\n", "events[drought].title_he", "Hebrew letters")]
    [InlineData("    text_he: לקצוב מים\n", "    text_he: '{nobody} קוצב מים'\n", "events[drought].options[ration].text_he", "placeholder")]
    // The review flag is agent-checked or absent.
    [InlineData("text_he_review: agent-checked\n", "text_he_review: reviewed\n", "events[drought].text_he_review", "agent-checked")]
    public void BadHebrewIsAContentError(string from, string to, string field, string message)
    {
        Assert.Contains(from, Hebrew);
        var ex = Bad(Hebrew.Replace(from, to));
        Assert.Equal("events/drought.yaml", ex.File);
        Assert.Equal(field, ex.Field);
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void AReviewFlagNeedsTheHebrewText()
    {
        var ex = Bad(ValidationTests.Event.Replace(Title, Title + "text_he_review: agent-checked\n"));
        Assert.Equal("events[drought].text_he", ex.Field);
    }
}
