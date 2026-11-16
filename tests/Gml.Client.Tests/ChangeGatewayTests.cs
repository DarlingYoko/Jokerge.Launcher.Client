using Gml.Web.Api.Domains.System;

namespace Gml.Client.Tests;

public class ChangeGatewayTests
{
    [Test]
    public void ChangeGateway_SwitchesHostUri()
    {
        using var manager = new GmlClientManager(Environment.CurrentDirectory, "https://launcher.example.com",
            new GameLoader(), "Test", OsType.Linux);

        manager.ChangeGateway(new Uri("https://launcher.ru.example.com"));

        Assert.That(manager.HostUri, Is.EqualTo(new Uri("https://launcher.ru.example.com")));
    }

    [TestCase("https://launcher.example.com/", "wss://launcher.example.com")]
    [TestCase("https://launcher.example.com:8443/", "wss://launcher.example.com:8443")]
    [TestCase("http://localhost:5000/", "ws://localhost:5000")]
    public void WebSocketAddress_FollowsGatewaySchemeHostAndPort(string gateway, string expected)
    {
        Assert.That(GmlClientManager.BuildWebSocketAddress(new Uri(gateway)), Is.EqualTo(expected));
    }
}
