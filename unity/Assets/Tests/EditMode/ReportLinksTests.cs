#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;

/// <summary>#1169: the two prefilled GitHub links ReportLinks builds. GitHub fills a YAML issue form by field id and ignores
/// `body=` and `labels=` there, so every key after the template must be a field id of that template file; the values are
/// URL-encoded and the whole URL stays under GitHub's 8000-character limit, however long the browser's user agent.</summary>
public class ReportLinksTests
{
    const string Issues = "https://github.com/eyalgolan/statecraft-israel/issues/new?";

    static string[] FieldIds(string template) =>
        Regex.Matches(File.ReadAllText(Path.Combine(Paths.RepoRoot, ".github", "ISSUE_TEMPLATE", template)), @"^\s+id:\s*(\S+)\s*$", RegexOptions.Multiline)
            .Cast<Match>().Select(m => m.Groups[1].Value).ToArray();

    static string[] Keys(string url) =>
        url.Substring(url.IndexOf('?') + 1).Split('&').Select(p => p.Split('=')[0]).ToArray();

    static string Value(string url, string key) =>
        Uri.UnescapeDataString(url.Substring(url.IndexOf('?') + 1).Split('&').Single(p => p.StartsWith(key + "=")).Substring(key.Length + 1));

    [Test]
    public void TheBugLinkFillsTheBugFormByItsFieldIds()
    {
        var url = ReportLinks.Bug("0.1.0 / content ab12", "Mozilla/5.0 (X11; Linux x86_64) Chrome/141 & co, screen 1920x1080");
        StringAssert.StartsWith(Issues + "template=bug_report.yml&", url);
        CollectionAssert.AreEqual(new[] { "template", "title", "version", "env" }, Keys(url));
        var ids = FieldIds("bug_report.yml");
        foreach (var key in Keys(url).Skip(2)) CollectionAssert.Contains(ids, key, $"{key} is a field id of bug_report.yml");
        // What the player writes stays empty: the form's own fields, never prefilled.
        CollectionAssert.IsSubsetOf(new[] { "what", "repro" }, ids);
        Assert.AreEqual(ReportLinks.BugTitle, Value(url, "title"));
        Assert.AreEqual("0.1.0 / content ab12", Value(url, "version"));
        Assert.AreEqual("Mozilla/5.0 (X11; Linux x86_64) Chrome/141 & co, screen 1920x1080", Value(url, "env"));
        // Each value encoded: a space, '/', '&', ';' and ':' never reach the query raw.
        StringAssert.Contains("&version=0.1.0%20%2F%20content%20ab12&", url);
        StringAssert.Contains("&env=Mozilla%2F5.0%20%28X11%3B%20Linux%20x86_64%29%20Chrome%2F141%20%26%20co%2C%20screen%201920x1080", url);
        StringAssert.DoesNotContain("body=", url);
        StringAssert.DoesNotContain("labels=", url);
    }

    [Test]
    public void TheFeatureLinkCarriesOnlyItsTitle()
    {
        var url = ReportLinks.Feature();
        Assert.AreEqual(Issues + "template=feature_request.yml&title=" + Uri.EscapeDataString(ReportLinks.FeatureTitle), url);
        Assert.IsTrue(File.Exists(Path.Combine(Paths.RepoRoot, ".github", "ISSUE_TEMPLATE", "feature_request.yml")));
        StringAssert.DoesNotContain("body=", url);
        StringAssert.DoesNotContain("labels=", url);
    }

    /// <summary>The version line is the app's version and the content hash GameBoot logs (`BOOT OK content ...`); the
    /// build has no commit SHA, so none is made up.</summary>
    [Test]
    public void TheVersionIsTheAppVersionAndTheContentHash()
    {
        Assert.AreEqual("0.1.0 / content 9f3a", ReportLinks.Version("0.1.0", "9f3a"));
    }

    /// <summary>A user agent of any length, even of characters that encode to nine each, keeps the URL under 8000
    /// characters; the clipped value is the one the panel shows (BugFields), so the player sees what is sent.</summary>
    [Test]
    public void TheUrlStaysUnderTheCapAndShowsWhatItCarries([Values("a", "ש", "😀")] string unit)
    {
        var huge = string.Concat(Enumerable.Repeat(unit, 10000));
        var url = ReportLinks.Bug(huge, huge);
        Assert.Less(url.Length, ReportLinks.MaxUrl, "the whole URL");
        var fields = ReportLinks.BugFields(huge, huge);
        Assert.AreEqual(fields.Single(f => f.id == "env").value, Value(url, "env"), "the panel shows the clipped value");
        Assert.AreEqual(fields.Single(f => f.id == "version").value, Value(url, "version"));
        Assert.IsFalse(char.IsHighSurrogate(fields.Single(f => f.id == "env").value.Last()), "a pair is never cut in half");
    }
}
