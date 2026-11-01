using System.Diagnostics;
using Statecraft.Cli;
using Statecraft.Sim;

/// <summary>#1911, R-ALT-011: `Statecraft.Cli --timeline-course SAVE` prints the played course from a save, row for row what
/// the Unity timeline screen (TimelinePresenter) shows, because both read Timeline.Entries. No new state, no save change.</summary>
public class TimelineCourseCliTests
{
    const string Header = "event\tchosen\thistorical\tdiverged\trejoined_by";

    /// <summary>The parent decision, its consequence card (whose option rejoins the record) and the record's follow-up.</summary>
    static string Root(string ruleset)
    {
        var files = ruleset == "legacy" ? new Dictionary<string, string>() : Fixture.Gdd();
        if (ruleset == "gdd-0.3")
        {
            files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
            files[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        }
        var rejoining = AlternateTimelineTests.Consequence.Replace("    text: Hold the course\n", "    text: Hold the course\n    rejoin: parent\n");
        return Fixture.Write(files, new Dictionary<string, string>
        {
            ["parent.yaml"] = AlternateTimelineTests.Parent,
            ["after_road.yaml"] = rejoining,
            ["on_record.yaml"] = AlternateTimelineTests.RecordFollowUp,
        });
    }

    /// <summary>Ends turns until <paramref name="ev"/> is pending, then answers it in a turn of its own.</summary>
    static void Choose(GameSession session, string ev, string option)
    {
        for (int turn = 0; turn < 30 && !session.State.PendingEvents.Contains(ev); turn++) session.Step(Array.Empty<Command>());
        Assert.Contains(ev, session.State.PendingEvents);
        Assert.Empty(session.Step(new Command[] { new ChooseEventOption(ev, option) }).Rejected);
    }

    /// <summary>What the timeline screen lists for a state, in the report's columns ("-" for an empty field).</summary>
    static string Expected(Content c, GameState s) =>
        string.Concat(Timeline.Entries(c, s).Select(e => $"{e.Event}\t{e.Chosen}\t{(e.Historical.Length == 0 ? "-" : e.Historical)}\t"
            + $"{(e.Diverged ? "true" : "false")}\t{(e.RejoinedBy.Length == 0 ? "-" : e.RejoinedBy)}\n"));

    /// <summary>Runs the compiled CLI (no nested build); the test host already holds the machine lock.</summary>
    static async Task<(int Exit, string Out, string Err)> Cli(params string[] args)
    {
        var info = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        info.ArgumentList.Add(typeof(InputParse).Assembly.Location);
        foreach (var a in args) info.ArgumentList.Add(a);
        using var child = Process.Start(info)!;
        child.StandardInput.Close();   // a CLI that ignored the flag would read commands; end of input makes it quit
        var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await child.WaitForExitAsync(limit.Token); }
        catch (OperationCanceledException) { child.Kill(entireProcessTree: true); throw; }
        return (child.ExitCode, (await stdout).Replace("\r\n", "\n"), (await stderr).Replace("\r\n", "\n"));
    }

    /// <summary>A writer that fails on the first write, as a closed pipe or full disk would.</summary>
    sealed class FailingWriter : StringWriter
    {
        readonly Exception failure;
        public FailingWriter(Exception failure) { this.failure = failure; }
        public override void Write(char value) => throw failure;
        public override void Write(string? value) => throw failure;
        public override void WriteLine(string? value) => throw failure;
    }

    [Theory]
    // R-ALT-011: the record, a departure, and a departure that a consequence card rejoined, under each ruleset.
    [InlineData("legacy", "record", false)]
    [InlineData("legacy", "road", false)]
    [InlineData("legacy", "road", true)]
    [InlineData("gdd-0.1", "record", false)]
    [InlineData("gdd-0.1", "road", true)]
    [InlineData("gdd-0.3", "record", false)]
    [InlineData("gdd-0.3", "road", true)]
    public void TheCourseIsTheTimelineScreensRowsAfterSaveAndReload(string ruleset, string choice, bool rejoin)
    {
        var root = Root(ruleset);
        var c = ContentLoader.Load(root);
        var played = GameSession.New(c, 1, Mode.Historical);
        Choose(played, "parent", choice);
        if (rejoin) Choose(played, "after_road", "hold");

        var parent = Timeline.Entries(c, played.State).Single(e => e.Event == "parent");
        Assert.Equal((choice, "record", choice == "road", rejoin ? "after_road" : ""), (parent.Chosen, parent.Historical, parent.Diverged, parent.RejoinedBy));

        var save = Path.Combine(root, "played.save");
        File.WriteAllText(save, played.Save.Write());
        var bytes = File.ReadAllBytes(save);
        var output = new StringWriter(); var errors = new StringWriter();
        Assert.Equal(0, TimelineCourse.Run(c, save, output, errors));
        Assert.Equal("", errors.ToString());
        Assert.Equal(Header + "\n" + Expected(c, played.State), output.ToString().Replace("\r\n", "\n"));
        Assert.Equal(bytes, File.ReadAllBytes(save));                    // the report never writes the save
        var reloaded = GameSession.Load(c, played.Save.Write(), out _);
        Assert.Equal(Expected(c, played.State), Expected(c, reloaded.State));   // a reload gives the screen the same rows
    }

    [Fact]
    // R-ALT-011: a game with no answered event has a header and no rows; an unanswered card is never a row.
    public void ANewGameHasOnlyTheHeader()
    {
        var root = Root("legacy");
        var c = ContentLoader.Load(root);
        var save = Path.Combine(root, "new.save");
        File.WriteAllText(save, GameSession.New(c, 1, Mode.Historical).Save.Write());
        var output = new StringWriter();
        Assert.Equal(0, TimelineCourse.Run(c, save, output, new StringWriter()));
        Assert.Equal(Header + "\n", output.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    // R-ALT-011: a save made with other content still reports, and says so on stderr (GameSession.Load's own warning).
    public void AContentChangeWarningGoesToStderrAndTheCourseStillPrints()
    {
        var root = Root("legacy");
        var c = ContentLoader.Load(root);
        var played = GameSession.New(c, 1, Mode.Historical);
        Choose(played, "parent", "road");
        var save = Path.Combine(root, "played.save");
        File.WriteAllText(save, played.Save.Write());
        var changedRoot = Root("legacy");
        var follow = Path.Combine(changedRoot, "events", "on_record.yaml");
        File.WriteAllText(follow, File.ReadAllText(follow).Replace("The next real decision.", "The next real decision, reworded."));
        var changed = ContentLoader.Load(changedRoot);
        Assert.NotEqual(c.Hash, changed.Hash);

        var output = new StringWriter(); var errors = new StringWriter();
        Assert.Equal(0, TimelineCourse.Run(changed, save, output, errors));
        Assert.Contains("Save warning: " + GameSession.ContentChanged, errors.ToString());
        Assert.StartsWith(Header + "\nparent\troad\trecord\ttrue\t-\n", output.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    // R-ALT-011: a file that cannot be read, and a save that does not parse, print no report and exit 1.
    public void AnUnreadableOrBrokenSaveIsRefusedWithoutAReport()
    {
        var root = Root("legacy");
        var c = ContentLoader.Load(root);

        var output = new StringWriter(); var errors = new StringWriter();
        Assert.Equal(1, TimelineCourse.Run(c, Path.Combine(root, "missing.save"), output, errors));
        Assert.Equal("", output.ToString());
        Assert.StartsWith("Save error: cannot read ", errors.ToString());

        var broken = Path.Combine(root, "broken.save");
        File.WriteAllText(broken, "not a save\n");
        output = new StringWriter(); errors = new StringWriter();
        Assert.Equal(1, TimelineCourse.Run(c, broken, output, errors));
        Assert.Equal("", output.ToString());
        Assert.StartsWith("Save error", errors.ToString());
        Assert.DoesNotContain("cannot read", errors.ToString());
    }

    [Theory]
    // R-ALT-011: a failure while writing the report is not a save error; it reaches the caller as itself.
    [InlineData(typeof(IOException))]
    [InlineData(typeof(ArgumentException))]
    public void AnOutputFailureIsNotReportedAsASaveError(Type kind)
    {
        var root = Root("legacy");
        var c = ContentLoader.Load(root);
        var save = Path.Combine(root, "new.save");
        File.WriteAllText(save, GameSession.New(c, 1, Mode.Historical).Save.Write());
        var failure = (Exception)Activator.CreateInstance(kind, "the output is closed")!;
        var errors = new StringWriter();
        var thrown = Assert.ThrowsAny<Exception>(() => TimelineCourse.Run(c, save, new FailingWriter(failure), errors));
        Assert.Same(failure, thrown);
        Assert.Equal("", errors.ToString());
    }

    [Fact]
    // R-ALT-011: the command line runs the report, refuses a missing path and refuses another mode beside it.
    public async Task TheCommandLineRunsTheReportAndRefusesMixedModes()
    {
        var root = Root("legacy");
        var c = ContentLoader.Load(root);
        var played = GameSession.New(c, 1, Mode.Historical);
        Choose(played, "parent", "road");
        var save = Path.Combine(root, "played.save");
        File.WriteAllText(save, played.Save.Write());

        var run = await Cli("--content", root, "--timeline-course", save);
        Assert.True(run.Exit == 0, run.Out + run.Err);
        Assert.Equal("", run.Err);
        Assert.Equal(Header + "\n" + Expected(c, played.State), run.Out);

        var missing = await Cli("--content", root, "--timeline-course");
        Assert.Equal(1, missing.Exit);
        Assert.Contains("Missing value for --timeline-course", missing.Err);

        var mixed = await Cli("--content", root, "--timeline-course", save, "--timeline-report");
        Assert.Equal(1, mixed.Exit);
        Assert.Equal("", mixed.Out);
        Assert.Contains("--timeline-course", mixed.Err);
    }
}
