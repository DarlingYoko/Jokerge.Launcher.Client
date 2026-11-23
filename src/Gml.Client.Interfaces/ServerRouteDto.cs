namespace Gml.Client.Interfaces;

/// <summary>
///     One entry point to a profile's game server (the direct domain, a bridge, another region). The API lists
///     them in order: the first route is the main address, the one kept in the player's server list.
/// </summary>
/// <remarks>Declared here because the published Gml.Dto package has no such type.</remarks>
public sealed class ServerRouteDto
{
    /// <summary>Shown to the player, e.g. «Напрямую» or «Франкфурт».</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Host name or IP the game connects to.</summary>
    public string Address { get; set; } = string.Empty;

    public int Port { get; set; }
}
