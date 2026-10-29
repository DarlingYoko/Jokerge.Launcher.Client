using Gml.Client.Presence;

namespace Gml.Client.Tests;

public class PlayerHeadUrlTests
{
    [Test]
    public void Build_PointsAtSkinServiceBehindProxy()
    {
        var url = PlayerHeadUrl.Build(new Uri("https://launcher.example.com/"), "Steve", 128);

        Assert.That(url, Is.EqualTo("https://launcher.example.com/skins/skin/Steve/head/128"));
    }

    [Test]
    public void Build_KeepsBasePathOfGateway()
    {
        var url = PlayerHeadUrl.Build(new Uri("https://example.com/jokerge/"), "Steve", 64);

        Assert.That(url, Is.EqualTo("https://example.com/jokerge/skins/skin/Steve/head/64"));
    }

    [Test]
    public void Build_EscapesPlayerName()
    {
        var url = PlayerHeadUrl.Build(new Uri("https://example.com/"), "a b/c?", 128);

        Assert.That(url, Is.EqualTo("https://example.com/skins/skin/a%20b%2Fc%3F/head/128"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Build_ReturnsNullWithoutPlayerName(string? name)
    {
        Assert.That(PlayerHeadUrl.Build(new Uri("https://example.com/"), name, 128), Is.Null);
    }

    // Discord fetches the image through its own proxy, so an address only reachable from the
    // player's machine can never load; fall back to the static image instead of sending it.
    [TestCase("http://localhost:5000/")]
    [TestCase("http://127.0.0.1/")]
    [TestCase("http://10.0.0.5/")]
    [TestCase("http://192.168.1.10/")]
    [TestCase("http://172.16.0.1/")]
    [TestCase("http://172.31.255.255/")]
    [TestCase("http://gml-proxy/")]
    public void Build_ReturnsNullForHostsDiscordCannotReach(string baseAddress)
    {
        Assert.That(PlayerHeadUrl.Build(new Uri(baseAddress), "Steve", 128), Is.Null);
    }

    [TestCase("http://172.15.0.1/")]
    [TestCase("http://172.32.0.1/")]
    [TestCase("http://8.8.8.8/")]
    [TestCase("http://play.example.com:8080/")]
    public void Build_AcceptsPublicHosts(string baseAddress)
    {
        Assert.That(PlayerHeadUrl.Build(new Uri(baseAddress), "Steve", 128), Is.Not.Null);
    }
}
