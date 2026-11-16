using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gml.Client.Helpers;

/// <summary>
/// Picks the backend address the launcher talks to when the same backend is published under
/// several names, e.g. a direct one and one relayed through another network for players whose
/// route to the direct one is blocked.
/// </summary>
public static class GatewaySelector
{
    /// <summary>
    /// Probes all <paramref name="ordered"/> candidates at once and returns the earliest one in that
    /// order that is reachable, or <c>null</c> when none is. Once a later candidate answers, earlier
    /// ones still pending get <paramref name="fallbackGrace"/> more before the later one is taken, so
    /// a blocked first choice costs the grace period rather than the full timeout. Each probe gets
    /// <paramref name="perHostTimeout"/>; a probe that throws or overruns counts as unreachable, and
    /// the timeout holds even if the probe ignores its token.
    /// </summary>
    public static async Task<Uri?> SelectAsync(IReadOnlyList<Uri> ordered,
        Func<Uri, CancellationToken, Task<bool>> probe, TimeSpan perHostTimeout, TimeSpan fallbackGrace,
        CancellationToken cancellationToken = default)
    {
        // Cancelled on return, so probes that lost the race stop instead of running to their timeout.
        using var losers = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            var probes = ordered.Select(uri => ProbeAsync(uri, probe, perHostTimeout, losers.Token)).ToArray();
            Task? graceExpired = null;

            while (true)
            {
                var best = BestSoFar(probes, out var undecidedBeforeBest);

                if (best is not null && !undecidedBeforeBest) return ordered[best.Value];
                if (best is not null && graceExpired is { IsCompleted: true }) return ordered[best.Value];
                if (best is null && probes.All(p => p.IsCompleted)) return null;

                if (best is not null && graceExpired is null)
                    graceExpired = Task.Delay(fallbackGrace, cancellationToken);

                var waitOn = probes.Where(p => !p.IsCompleted).Cast<Task>().ToList();
                if (graceExpired is not null) waitOn.Add(graceExpired);

                await Task.WhenAny(waitOn).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        finally
        {
            losers.Cancel();
        }
    }

    /// <summary>Same scheme, host, port and path, ignoring case and a trailing slash.</summary>
    public static bool SameGateway(Uri a, Uri b)
    {
        return string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(Uri uri)
    {
        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    // Index of the earliest candidate known to be reachable, and whether any candidate before it is
    // still undecided.
    private static int? BestSoFar(IReadOnlyList<Task<bool>> probes, out bool undecidedBeforeBest)
    {
        undecidedBeforeBest = false;

        for (var i = 0; i < probes.Count; i++)
        {
            if (!probes[i].IsCompleted)
            {
                undecidedBeforeBest = true;
                continue;
            }

            if (probes[i].Result) return i;
        }

        return null;
    }

    // Never throws: unreachable, failed and overrun probes all come back as false.
    private static async Task<bool> ProbeAsync(Uri candidate, Func<Uri, CancellationToken, Task<bool>> probe,
        TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        Task<bool> attempt;
        try
        {
            attempt = probe(candidate, cts.Token);
        }
        catch (Exception)
        {
            return false;
        }

        var deadline = Task.Delay(Timeout.Infinite, cts.Token);
        var finished = await Task.WhenAny(attempt, deadline).ConfigureAwait(false);

        if (finished != attempt)
        {
            // Observe the abandoned probe's eventual failure so it doesn't surface as an
            // unobserved task exception.
            _ = attempt.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            return false;
        }

        cts.Cancel();

        try
        {
            return await attempt.ConfigureAwait(false);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
