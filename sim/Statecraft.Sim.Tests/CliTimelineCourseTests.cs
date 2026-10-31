using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Statecraft.Sim;

/// <summary>#1911: the developer CLI inspects the same resolved course as TimelinePresenter, without new save state.</summary>
[Collection(nameof(CliTimelineCourseTests))]
public class CliTimelineCourseTests
{
    const string Header = "event\tchosen\thistorical\tdiverged\trejoined_by";

    static (string Root, Content Content) CourseContent(string ruleset = "legacy")
    {
        var files = ruleset == "legacy" ? new Dictionary<string, string>() : Fixture.Gdd();
        if (ruleset == "gdd-0.3")
        {
            files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
            files[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        }
        var consequence = AlternateTimelineTests.Consequence.Replace("    text: Hold the course\n",
            "    text: Hold the course\n    rejoin: parent\n");
        var root = Fixture.Write(files, events: new Dictionary<string, string>
        {
            ["parent.yaml"] = AlternateTimelineTests.Parent,
            ["after_road.yaml"] = consequence,
            ["on_record.yaml"] = AlternateTimelineTests.RecordFollowUp,
        });
        return (root, ContentLoader.Load(root));
    }

    static void Answer(GameSession session, string id, string option)
    {
        for (int i = 0; i < 20 && !session.State.PendingEvents.Contains(id); i++) session.Step(Array.Empty<Command>());
        Assert.Contains(id, session.State.PendingEvents);
        Assert.Empty(session.Step(new Command[] { new ChooseEventOption(id, option) }).Rejected);
        Assert.Equal(option, session.State.ChosenOptions[id]);
    }

    static async Task<(int Exit, string Out, string Error)> Cli(string content, params string[] args)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in new[] { typeof(Statecraft.Cli.InputParse).Assembly.Location, "--content", content }.Concat(args))
            info.ArgumentList.Add(arg);
        // Execute the compiled CLI, never a nested build. Closing stdin also bounds the old CLI when it ignores a flag.
        using var child = Process.Start(info)!;
        child.StandardInput.Close();
        var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await child.WaitForExitAsync(limit.Token); }
        catch (OperationCanceledException) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); throw; }
        return (child.ExitCode, (await stdout).Replace("\r\n", "\n"), (await stderr).Replace("\r\n", "\n"));
    }

    static string[] Rows(IEnumerable<Timeline.TimelineEntry> entries) => entries.Select(e =>
        string.Join("\t", e.Event, e.Chosen, e.Historical == "" ? "-" : e.Historical,
            e.Diverged ? "true" : "false", e.RejoinedBy == "" ? "-" : e.RejoinedBy)).ToArray();

    // R-ALT-009, R-ALT-011: the UI reads Timeline.Entries; departure stays recorded even after a rejoin.
    [Theory]
    [InlineData("legacy", "record", false)]
    [InlineData("legacy", "road", false)]
    [InlineData("legacy", "road", true)]
    [InlineData("gdd-0.1", "record", false)]
    [InlineData("gdd-0.1", "road", false)]
    [InlineData("gdd-0.1", "road", true)]
    [InlineData("gdd-0.3", "record", false)]
    [InlineData("gdd-0.3", "road", false)]
    [InlineData("gdd-0.3", "road", true)]
    public async Task ReportAgreesWithTheTimelineBeforeAndAfterSaveReload(string ruleset, string choice, bool rejoin)
    {
        var (root, content) = CourseContent(ruleset);
        var played = GameSession.New(content, 1, Mode.Historical);
        Answer(played, "parent", choice);
        if (rejoin) Answer(played, "after_road", "hold");
        var entries = Timeline.Entries(content, played.State);
        var parent = entries.Single(e => e.Event == "parent");
        Assert.Equal(choice, parent.Chosen);
        Assert.Equal(choice != "record", parent.Diverged);
        Assert.Equal(rejoin ? "after_road" : "", parent.RejoinedBy);
        Assert.DoesNotContain(entries, e => e.Event == "on_record"); // unresolved cards never become played rows
        var loaded = GameSession.Load(content, played.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(played.State.Hash(), loaded.State.Hash());
        Assert.Equal(Rows(entries), Rows(Timeline.Entries(content, loaded.State)));
        var save = Path.Combine(root, "course.save");
        File.WriteAllText(save, loaded.Save.Write());
        var before = File.ReadAllBytes(save);
        var result = await Cli(root, "--timeline-course", save);
        Assert.True(result.Exit == 0, result.Out + result.Error);
        Assert.Equal("", result.Error);
        var lines = result.Out.TrimEnd('\n').Split('\n');
        Assert.Equal(Header, lines[0]);
        Assert.Equal(Rows(entries), lines.Skip(1));
        Assert.Equal(before, File.ReadAllBytes(save));
    }

    // R-ALT-011: a new game has no resolved course, even when content offers alternate roads.
    [Theory]
    [InlineData("legacy")]
    [InlineData("gdd-0.1")]
    [InlineData("gdd-0.3")]
    public async Task AnUnplayedSaveHasOnlyTheHeader(string ruleset)
    {
        var (root, content) = CourseContent(ruleset);
        var save = Path.Combine(root, "new.save");
        File.WriteAllText(save, GameSession.New(content, 1, Mode.Historical).Save.Write());
        var result = await Cli(root, "--timeline-course", save);
        Assert.Equal(0, result.Exit);
        Assert.Equal("", result.Error);
        Assert.Equal(Header + "\n", result.Out);
    }

    // R-ALT-011: the ordinary load warning is retained, instead of certifying a different content version as the original.
    [Fact]
    public async Task AContentMismatchIsReportedWithoutChangingTheSaveOrCourse()
    {
        var (root, content) = CourseContent();
        var played = GameSession.New(content, 1, Mode.Historical);
        Answer(played, "parent", "road");
        var save = Path.Combine(root, "changed-content.save");
        File.WriteAllText(save, played.Save.Write().Replace("content " + content.Hash, "content " + new string('0', 64)));
        var before = File.ReadAllBytes(save);
        var result = await Cli(root, "--timeline-course", save);
        Assert.Equal(0, result.Exit);
        Assert.Equal("Save warning: " + GameSession.ContentChanged + "\n", result.Error);
        Assert.Equal(new[] { Header }.Concat(Rows(Timeline.Entries(content, played.State))), result.Out.TrimEnd('\n').Split('\n'));
        Assert.Equal(before, File.ReadAllBytes(save));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("damaged")]
    [InlineData("newer")]
    [InlineData("empty")]
    [InlineData("directory")]
    public async Task ARefusedSaveProducesNoCourse(string kind)
    {
        var (root, content) = CourseContent();
        var save = kind == "empty" ? "" : kind == "directory" ? root : Path.Combine(root, "refused.save");
        if (kind == "damaged") File.WriteAllText(save, "not a save\n");
        if (kind == "newer") File.WriteAllText(save, GameSession.New(content, 1, Mode.Historical).Save.Write()
            .Replace("ruleset legacy\n", "ruleset future\n"));
        var result = await Cli(root, "--timeline-course", save);
        Assert.Equal(1, result.Exit);
        Assert.Equal("", result.Out);
        Assert.StartsWith("Save error", result.Error);
        if (kind == "newer") Assert.Contains(SaveException.NewerVersion, result.Error);
    }

    [Fact]
    public async Task ACourseFlagNeedsASavePath()
    {
        var (root, _) = CourseContent();
        var result = await Cli(root, "--timeline-course");
        Assert.Equal(1, result.Exit);
        Assert.Equal("", result.Out);
        Assert.Equal("Missing value for --timeline-course\n", result.Error);
    }

    [Fact]
    public async Task ACourseReportDoesNotSilentlyReplaceTheAuthorCoverageReport()
    {
        var (root, content) = CourseContent();
        var save = Path.Combine(root, "new.save");
        File.WriteAllText(save, GameSession.New(content, 1, Mode.Historical).Save.Write());
        var result = await Cli(root, "--timeline-course", save, "--timeline-report");
        Assert.Equal(1, result.Exit);
        Assert.Equal("", result.Out);
        Assert.Contains("--timeline-course", result.Error);
        Assert.Contains("--timeline-report", result.Error);
    }

    // R-ALT-011: output failures must not be misreported as unreadable saves.
    [Theory]
    [InlineData("argument")]
    [InlineData("io")]
    public void AReportOutputFailureIsNotASaveReadError(string kind)
    {
        var (root, content) = CourseContent();
        var save = Path.Combine(root, "output-failure.save");
        File.WriteAllText(save, GameSession.New(content, 1, Mode.Historical).Save.Write());
        Exception failure = kind == "argument" ? new ArgumentException("test output failure") : new IOException("test output failure");
        var stdout = Console.Out; var stderr = Console.Error;
        var culture = CultureInfo.CurrentCulture; var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        using var error = new StringWriter();
        try
        {
            Console.SetOut(new FailingCourseOutput(stdout, failure));
            Console.SetError(error);
            var entry = typeof(Statecraft.Cli.InputParse).Assembly.EntryPoint!;
            var exception = Assert.Throws<TargetInvocationException>(() => entry.Invoke(null,
                new object[] { new[] { "--content", root, "--timeline-course", save } }));
            Assert.Same(failure, exception.InnerException);
            Assert.Equal("", error.ToString());
        }
        finally
        {
            Console.SetOut(stdout); Console.SetError(stderr);
            CultureInfo.DefaultThreadCurrentCulture = defaultCulture; CultureInfo.CurrentCulture = culture;
        }
    }

    sealed class FailingCourseOutput : TextWriter
    {
        readonly TextWriter inner; readonly Exception failure;
        public FailingCourseOutput(TextWriter inner, Exception failure) { this.inner = inner; this.failure = failure; }
        public override System.Text.Encoding Encoding => inner.Encoding;
        public override void Write(char value) => inner.Write(value);
        public override void Write(string? value) => inner.Write(value);
        public override void WriteLine(string? value)
        {
            if (value == Header) throw failure;
            inner.WriteLine(value);
        }
    }

    // R-ALT-011: existing-behavior guard, already passing before --timeline-course was added.
    [Fact]
    public async Task TheAuthorCoverageReportKeepsItsExistingOutput()
    {
        var (root, _) = CourseContent();
        var result = await Cli(root, "--timeline-report");
        Assert.Equal(0, result.Exit);
        Assert.Equal("", result.Error);
        Assert.Equal("parent/road  basis=proposed  cards=after_road  variants=-  breaks=on_record\n", result.Out);
    }
}

// These tests redirect process-wide Console writers; they must not overlap other test collections.
[CollectionDefinition(nameof(CliTimelineCourseTests), DisableParallelization = true)]
public sealed class CliTimelineCourseConsoleCollection { }
