using System.Net;
using System.Net.Http;
using Gml.Client.Helpers;
using Gml.Web.Api.Domains.System;

namespace Gml.Client.Tests;

/// <summary>
///     The panel can say which optional mods an optional mod needs; the launcher reads that together with the
///     mod's title and description.
/// </summary>
public class OptionalModDetailsRequestTests
{
    private static ApiProcedures Api(string body) =>
        new(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            }))
            { BaseAddress = new Uri("https://launcher.test/") },
            OsType.Windows);

    [Test]
    public async Task GetOptionalModsInfo_ReadsDependencies()
    {
        var api = Api("""
            {"data":[
              {"key":"a-optional-mod.jar","title":"A","description":"d","dependencies":["b-optional-mod.jar"]}
            ],"message":"ok","statusCode":200}
            """);

        var details = await api.GetOptionalModsInfo("player-token");

        Assert.That(details.Data?.Single().Dependencies, Is.EqualTo(new[] { "b-optional-mod.jar" }));
    }

    [TestCase("""{"key":"a-optional-mod.jar","title":"A","description":"d"}""")]
    [TestCase("""{"key":"a-optional-mod.jar","title":"A","description":"d","dependencies":null}""")]
    public async Task GetOptionalModsInfo_OlderApi_MeansNoDependencies(string mod)
    {
        var api = Api($$"""{"data":[{{mod}}],"message":"ok","statusCode":200}""");

        var details = await api.GetOptionalModsInfo("player-token");

        Assert.That(details.Data?.Single().Dependencies, Is.Not.Null.And.Empty);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
