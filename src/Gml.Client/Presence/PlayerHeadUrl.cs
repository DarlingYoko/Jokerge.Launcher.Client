using System.Net;

namespace Gml.Client.Presence;

public static class PlayerHeadUrl
{
    /// <summary>
    ///     Public URL of the player's head, rendered by Skin.Service behind the proxy's <c>/skins</c> route.
    ///     Null when there is no name or when Discord's image proxy could not reach the host anyway.
    /// </summary>
    public static string? Build(Uri gateway, string? playerName, int size)
    {
        if (string.IsNullOrWhiteSpace(playerName) || !IsReachableByDiscord(gateway))
            return null;

        var root = gateway.AbsoluteUri.TrimEnd('/');

        return $"{root}/skins/skin/{Uri.EscapeDataString(playerName.Trim())}/head/{size}";
    }

    private static bool IsReachableByDiscord(Uri gateway)
    {
        if (gateway.IsLoopback)
            return false;

        if (IPAddress.TryParse(gateway.Host, out var address))
            return !IsPrivate(address);

        // A single-label name ("gml-proxy") only resolves inside the local network.
        return gateway.Host.Contains('.');
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;

        var bytes = address.GetAddressBytes();

        return bytes[0] is 10 or 127
               || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
               || (bytes[0] == 192 && bytes[1] == 168)
               || (bytes[0] == 169 && bytes[1] == 254);
    }
}
