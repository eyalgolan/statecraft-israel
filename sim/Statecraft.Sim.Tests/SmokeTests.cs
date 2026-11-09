using Statecraft.Sim;

public class SmokeTests
{
    [Fact]
    public void SimLibraryIsReferenced() => Assert.Equal("0.4.0", SimInfo.Version);
}
