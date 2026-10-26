using System.Text;
using Gml.Client.Presence;

namespace Gml.Client.Tests;

public class PresenceTests
{
    [Test]
    public void TryParseDimension_ReadsDimensionFromModLogLine()
    {
        const string line =
            "[12:00:00] [Render thread/INFO] (jokerge-presence) [JokergePresence] dimension=minecraft:the_nether";

        var parsed = PresenceLogParser.TryParseDimension(line, out var dimension);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(dimension, Is.EqualTo("minecraft:the_nether"));
        });
    }

    [Test]
    public void TryParseDimension_ReadsDimensionFromXmlLogLine()
    {
        // Launch arguments with Mojang's log4j XML config wrap every message in an XML event.
        const string line =
            "<log4j:Message><![CDATA[[JokergePresence] dimension=minecraft:the_end]]></log4j:Message>";

        var parsed = PresenceLogParser.TryParseDimension(line, out var dimension);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(dimension, Is.EqualTo("minecraft:the_end"));
        });
    }

    [Test]
    public void TryParseDimension_NoneMeansNotInWorld()
    {
        var parsed = PresenceLogParser.TryParseDimension("[JokergePresence] dimension=none", out var dimension);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(dimension, Is.Null);
        });
    }

    [TestCase("[12:00:00] [Render thread/INFO]: Loaded 1234 advancements")]
    [TestCase("")]
    [TestCase("[JokergePresence] dimension=")]
    public void TryParseDimension_IgnoresOtherLines(string line)
    {
        Assert.That(PresenceLogParser.TryParseDimension(line, out _), Is.False);
    }

    [TestCase("minecraft:overworld", PresenceDimension.Overworld)]
    [TestCase("minecraft:the_nether", PresenceDimension.Nether)]
    [TestCase("minecraft:the_end", PresenceDimension.End)]
    [TestCase("twilightforest:twilight_forest", PresenceDimension.Other)]
    [TestCase(null, PresenceDimension.Menu)]
    public void Classify_MapsDimensionIds(string? dimension, PresenceDimension expected)
    {
        Assert.That(PresenceLogParser.Classify(dimension), Is.EqualTo(expected));
    }

    [Test]
    public void Fit_KeepsShortText()
    {
        Assert.That(DiscordText.Fit("Играет на «Выживание»"), Is.EqualTo("Играет на «Выживание»"));
    }

    [Test]
    public void Fit_TruncatesToDiscordByteLimit()
    {
        var text = new string('ж', 100); // 200 UTF-8 bytes

        var fitted = DiscordText.Fit(text);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetByteCount(fitted!), Is.LessThanOrEqualTo(DiscordText.MaxBytes));
            Assert.That(fitted, Does.EndWith("…"));
            Assert.That(fitted, Does.StartWith("жжж"));
        });
    }

    [Test]
    public void Fit_DoesNotSplitSurrogatePairs()
    {
        var text = "a" + string.Concat(Enumerable.Repeat("😀", 60)); // 1 + 240 bytes

        var fitted = DiscordText.Fit(text)!;

        Assert.That(fitted.Where(char.IsSurrogate).Count() % 2, Is.Zero);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Fit_ReturnsNullForEmpty(string? text)
    {
        Assert.That(DiscordText.Fit(text), Is.Null);
    }
}
