using Gml.Dto.Profile;

namespace Gml.Client.Interfaces;

/// <summary>
/// <see cref="ProfileReadDto"/> plus the Jokerge-only fields the API returns but the published
/// Gml.Dto package doesn't declare yet.
/// </summary>
public class LauncherProfileReadDto : ProfileReadDto
{
    /// <summary>Profile name for Discord Rich Presence; empty means use <see cref="ProfileReadDto.DisplayName"/>.</summary>
    public string? DiscordName { get; set; }

    /// <summary>Discord asset key or https URL shown while playing this profile.</summary>
    public string? DiscordImage { get; set; }
}
