using System.Net;
using System.Net.Http;
using Gml.Client.Helpers;
using Gml.Web.Api.Domains.System;

namespace Gml.Client.Tests;

/// <summary>
///     The news list requires the player's token (integrations.news.view), but the launcher asked
///     for it anonymously, so the API answered 401 and the home page news block stayed empty.
/// </summary>
public class NewsRequestTests
{
    [Test]
    public async Task GetNews_SendsPlayerToken()
    {
        HttpRequestMessage? sent = null;
        var handler = new StubHandler(request =>
        {
            sent = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"title":"Patch","content":"notes"}]}""")
            };
        });
        var api = new ApiProcedures(new HttpClient(handler) { BaseAddress = new Uri("https://launcher.test/") },
            OsType.Windows);

        var news = await api.GetNews("player-token");

        Assert.Multiple(() =>
        {
            Assert.That(sent?.RequestUri?.AbsolutePath, Is.EqualTo("/api/v1/integrations/news/list"));
            Assert.That(sent?.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
            Assert.That(sent?.Headers.Authorization?.Parameter, Is.EqualTo("player-token"));
            Assert.That(news.Data, Has.Count.EqualTo(1));
        });
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
