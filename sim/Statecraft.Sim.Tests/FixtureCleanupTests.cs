using System;
using System.IO;
using Xunit.Sdk;

// Disposing the framework removes the whole per-run fixture root, so this must not run beside tests that
// still read their fixture folders: a DisableParallelization collection runs after the parallel ones, alone.
[CollectionDefinition("FixtureCleanup", DisableParallelization = true)]
public class FixtureCleanupCollection { }

[Collection("FixtureCleanup")]
public class FixtureCleanupTests
{
    [Fact]
    public void EndOfRunTeardownRemovesTheFoldersWriteCreated()
    {
        var dir = Fixture.Write();
        Assert.True(Directory.Exists(dir));
        ((IDisposable)new FixtureCleanupFramework(new NullMessageSink())).Dispose();
        Assert.False(Directory.Exists(dir));
    }
}
