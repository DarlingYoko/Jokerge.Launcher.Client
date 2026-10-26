namespace Gml.Client.Presence;

public enum PresenceDimension
{
    Menu,
    Overworld,
    Nether,
    End,
    Other
}

/// <summary>
///     Reads the dimension markers the Jokerge Presence mod writes to the game's stdout:
///     <c>[JokergePresence] dimension=&lt;id&gt;</c>, or <c>dimension=none</c> when the player leaves a world.
/// </summary>
public static class PresenceLogParser
{
    private const string Marker = "[JokergePresence] dimension=";
    private const string NoDimension = "none";

    /// <returns><c>true</c> when the line is a marker; <paramref name="dimension"/> is <c>null</c> outside a world.</returns>
    public static bool TryParseDimension(string? line, out string? dimension)
    {
        dimension = null;

        if (string.IsNullOrEmpty(line))
            return false;

        var index = line.IndexOf(Marker, StringComparison.Ordinal);

        if (index < 0)
            return false;

        // Read only identifier characters: the line may continue with XML (log4j XML layout) or other text.
        var start = index + Marker.Length;
        var end = start;

        while (end < line.Length && IsIdentifierChar(line[end]))
            end++;

        if (end == start)
            return false;

        var value = line.Substring(start, end - start);

        dimension = value == NoDimension ? null : value;
        return true;
    }

    /// <summary>Characters allowed in a Minecraft resource location (<c>namespace:path</c>).</summary>
    private static bool IsIdentifierChar(char c) =>
        c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-' or '.' or '/' or ':';

    public static PresenceDimension Classify(string? dimension) => dimension switch
    {
        null => PresenceDimension.Menu,
        "minecraft:overworld" => PresenceDimension.Overworld,
        "minecraft:the_nether" => PresenceDimension.Nether,
        "minecraft:the_end" => PresenceDimension.End,
        _ => PresenceDimension.Other
    };
}
