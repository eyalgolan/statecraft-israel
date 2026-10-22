using Statecraft.Sim;

public class SmokeTests
{
    [Fact]
    public void SimLibraryIsReferenced() => Assert.Equal("0.2.0", SimInfo.Version);
}
