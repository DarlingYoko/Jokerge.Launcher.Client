using System.Net;
using System.Net.Http;
using Gml.Client.Helpers;
using Gml.Web.Api.Domains.System;

namespace Gml.Client.Tests;

/// <summary>
///     A profile's server can be reached through several entry points (direct domain, bridges). The launcher
///     asks the API for them with the player's token; the first route is the address in the server list.
/// </summary>
public class ServerRoutesRequestTests
{
    private static ApiProcedures Api(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("https://launcher.test/") },
            OsType.Windows);

    [Test]
    public async Task GetServerRoutes_SendsPlayerToken_AndKeepsTheOrder()
    {
        HttpRequestMessage? sent = null;
        var api = Api(request =>
        {
            sent = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"data":[
                      {"name":"Напрямую","address":"waffle.jokerge.com","port":25565},
                      {"name":"Франкфурт","address":"fra.jokerge.com","port":25566}
                    ],"message":"ok","statusCode":200}
                    """)
            };
        });

        var routes = await api.GetServerRoutes("Waffle Heart", "player-token");

        Assert.Multiple(() =>
        {
            Assert.That(sent?.RequestUri?.AbsolutePath, Is.EqualTo("/api/v1/servers/Waffle%20Heart/routes"));
            Assert.That(sent?.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(sent?.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
            Assert.That(sent?.Headers.Authorization?.Parameter, Is.EqualTo("player-token"));
            Assert.That(routes.Data?.Select(r => r.Name), Is.EqualTo(new[] { "Напрямую", "Франкфурт" }));
            Assert.That(routes.Data?.Select(r => r.Address),
                Is.EqualTo(new[] { "waffle.jokerge.com", "fra.jokerge.com" }));
            Assert.That(routes.Data?.Select(r => r.Port), Is.EqualTo(new[] { 25565, 25566 }));
        });
    }

    [Test]
    public async Task GetServerRoutes_NoServers_IsAnEmptyList()
    {
        var api = Api(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"data":[],"message":"ok","statusCode":200}""")
        });

        var routes = await api.GetServerRoutes("Waffle", "player-token");

        Assert.That(routes.Data, Is.Not.Null.And.Empty);
    }

    [Test]
    public async Task GetServerRoutes_UnknownProfile_HasNoData()
    {
        var api = Api(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"data":null,"message":"not found","statusCode":404}""")
        });

        var routes = await api.GetServerRoutes("Missing", "player-token");

        Assert.That(routes.Data, Is.Null);
    }

    [Test]
    public async Task GetServerRoutes_ApiUnreachable_HasNoDataAndDoesNotThrow()
    {
        var api = Api(_ => throw new HttpRequestException("connection refused"));

        var routes = await api.GetServerRoutes("Waffle", "player-token");

        Assert.That(routes.Data, Is.Null);
    }

    [Test]
    public async Task GetServerRoutes_BrokenBody_HasNoDataAndDoesNotThrow()
    {
        var api = Api(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>bad gateway</html>")
        });

        var routes = await api.GetServerRoutes("Waffle", "player-token");

        Assert.That(routes.Data, Is.Null);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
