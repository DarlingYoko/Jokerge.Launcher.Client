using System.Collections.Generic;

namespace Gml.Client.Interfaces;

/// <summary>
///     What the panel says about an optional mod: its name for players, description and the optional mods it needs.
///     Stored under the jar file name (<see cref="Key" />, e.g. <c>sodium-optional-mod.jar</c>).
/// </summary>
/// <remarks>Declared here because the published Gml.Dto package has no dependencies field.</remarks>
public sealed class OptionalModDetailsDto
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Keys of the optional mods turned on together with this one; the player cannot turn them off.</summary>
    public List<string> Dependencies
    {
        get => _dependencies;
        set => _dependencies = value ?? []; // older APIs send no list or null
    }

    private List<string> _dependencies = [];
}
