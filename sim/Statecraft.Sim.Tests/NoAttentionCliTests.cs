using System.Diagnostics;
using Statecraft.Sim;

public class NoAttentionCliTests
{
    [Theory]
    [InlineData("legacy", true)]
    [InlineData("gdd-0.1", true)]
    [InlineData("gdd-0.3", false)]
    public async Task BriefingOnlyShowsAttentionWhenTheRulesetUsesIt(string ruleset, bool attention)
    {
        var files = ruleset == "legacy" ? new Dictionary<string, string>() : Fixture.Gdd();
        if (ruleset == "gdd-0.3")
        {
            files["eras.yaml"] = files["eras.yaml"].Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
            files[Gdd03BalanceCatalogue.FileName] = File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), Gdd03BalanceCatalogue.FileName));
        }
        var info = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var arg in new[] { typeof(Statecraft.Cli.InputParse).Assembly.Location, "--content", Fixture.Write(files), "--auto", "historical", "--turns", "1" }) info.ArgumentList.Add(arg);
        // Runs the compiled CLI, never a nested build/test or a build server. The test host owns the shared machine lock.
        using var child = Process.Start(info)!;
        var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await child.WaitForExitAsync(limit.Token); }
        catch (OperationCanceledException) { child.Kill(entireProcessTree: true); throw; }
        var output = await stdout; var error = await stderr;
        Assert.True(child.ExitCode == 0, output + error);
        var briefing = output.Split('\n').Single(x => x.StartsWith("== ", StringComparison.Ordinal));
        Assert.Equal(attention, briefing.Contains(" | attention ", StringComparison.Ordinal));
        Assert.Contains(" | security level ", briefing);
        if (attention) Assert.EndsWith(" | attention 3.0", briefing.TrimEnd('\r'));
    }
}
