using Statecraft.Sim;

public class SmokeTests
{
    [Fact]
    public void SimLibraryIsReferenced() => Assert.Equal("0.3.5", SimInfo.Version);
}
