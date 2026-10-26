using System.Reflection;
using NUnit.Framework;
using Statecraft.Game.UI;
public sealed class WebCompanionWiringTests
{
    [Test] public void ProductionRootOwnsCompanionLifetime()
    {
        var field = typeof(GameRoot).GetField("companion", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "the actual root must own the outbound companion");
        Assert.AreEqual("WebCompanion", field.FieldType.Name);
    }
}
