using System.Diagnostics;
using Gml.Client.Helpers;

namespace Gml.Client.Tests;

public class GatewaySelectorTests
{
    private static readonly Uri Direct = new("https://launcher.example.com");
    private static readonly Uri Bridge = new("https://launcher.ru.example.com");
    private static readonly Uri[] Candidates = [Direct, Bridge];

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Grace = TimeSpan.FromMilliseconds(300);

    [Test]
    public async Task Select_PrefersDirect_EvenWhenBridgeAnswersFirst()
    {
        var chosen = await GatewaySelector.SelectAsync(Candidates, async (uri, token) =>
        {
            if (uri == Direct) await Task.Delay(100, token);
            return true;
        }, Timeout, Grace);

        Assert.That(chosen, Is.EqualTo(Direct));
    }

    [Test]
    public async Task Select_ProbesAllCandidatesAtOnce()
    {
        var bothStarted = new TaskCompletionSource();
        var started = 0;

        var chosen = await GatewaySelector.SelectAsync(Candidates, async (_, _) =>
        {
            if (Interlocked.Increment(ref started) == Candidates.Length) bothStarted.TrySetResult();
            await bothStarted.Task;
            return true;
        }, TimeSpan.FromSeconds(2), Grace);

        Assert.That(chosen, Is.EqualTo(Direct));
    }

    [Test]
    public async Task Select_FallsBack_WhenDirectReportsUnreachable()
    {
        var chosen = await GatewaySelector.SelectAsync(Candidates,
            (uri, _) => Task.FromResult(uri == Bridge), Timeout, Grace);

        Assert.That(chosen, Is.EqualTo(Bridge));
    }

    [Test]
    public async Task Select_FallsBack_WhenDirectThrows()
    {
        var chosen = await GatewaySelector.SelectAsync(Candidates, (uri, _) =>
            uri == Direct
                ? Task.FromException<bool>(new HttpRequestException("connection refused"))
                : Task.FromResult(true), Timeout, Grace);

        Assert.That(chosen, Is.EqualTo(Bridge));
    }

    [Test]
    public async Task Select_FallsBackAfterGrace_WhenDirectHangs_NotAfterFullTimeout()
    {
        var hang = new TaskCompletionSource<bool>();
        var clock = Stopwatch.StartNew();

        var chosen = await GatewaySelector.SelectAsync(Candidates,
            (uri, _) => uri == Direct ? hang.Task : Task.FromResult(true), Timeout, Grace);

        Assert.That(chosen, Is.EqualTo(Bridge));
        Assert.That(clock.Elapsed, Is.LessThan(TimeSpan.FromSeconds(3)));
    }

    [Test]
    public async Task Select_TakesDirect_WhenItAnswersWithinGraceAfterBridge()
    {
        var chosen = await GatewaySelector.SelectAsync(Candidates, async (uri, token) =>
        {
            if (uri == Direct) await Task.Delay(100, token);
            return true;
        }, Timeout, TimeSpan.FromSeconds(2));

        Assert.That(chosen, Is.EqualTo(Direct));
    }

    [Test]
    public async Task Select_ReturnsNull_WhenNothingReachable()
    {
        var chosen = await GatewaySelector.SelectAsync(Candidates,
            (_, _) => Task.FromResult(false), Timeout, Grace);

        Assert.That(chosen, Is.Null);
    }

    [Test]
    public async Task Select_GivesUpAtTimeout_AndCancelsHangingProbes_EvenIfTheyIgnoreTheToken()
    {
        var tokens = new List<CancellationToken>();
        var clock = Stopwatch.StartNew();

        var chosen = await GatewaySelector.SelectAsync(Candidates, (_, token) =>
        {
            lock (tokens) tokens.Add(token);
            return new TaskCompletionSource<bool>().Task;
        }, TimeSpan.FromMilliseconds(200), Grace);

        Assert.That(chosen, Is.Null);
        Assert.That(clock.Elapsed, Is.LessThan(TimeSpan.FromSeconds(3)));
        Assert.That(tokens.All(t => t.IsCancellationRequested), Is.True);
    }

    [Test]
    public void SameGateway_IgnoresTrailingSlashAndCase()
    {
        Assert.That(GatewaySelector.SameGateway(new Uri("https://LAUNCHER.example.com/"), Direct), Is.True);
        Assert.That(GatewaySelector.SameGateway(Bridge, Direct), Is.False);
    }
}
