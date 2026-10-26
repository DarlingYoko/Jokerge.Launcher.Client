using System.Text;

namespace Gml.Client.Presence;

public static class DiscordText
{
    /// <summary>Discord rejects details/state/image text longer than 128 UTF-8 bytes.</summary>
    public const int MaxBytes = 128;

    private const string Ellipsis = "…";

    public static string? Fit(string? text, int maxBytes = MaxBytes)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();

        if (Encoding.UTF8.GetByteCount(text) <= maxBytes)
            return text;

        var budget = maxBytes - Encoding.UTF8.GetByteCount(Ellipsis);
        var builder = new StringBuilder();
        var used = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var length = char.IsHighSurrogate(text[i]) && i + 1 < text.Length ? 2 : 1;
            var bytes = Encoding.UTF8.GetByteCount(text.Substring(i, length));

            if (used + bytes > budget)
                break;

            builder.Append(text, i, length);
            used += bytes;
            i += length - 1;
        }

        return builder.ToString().TrimEnd() + Ellipsis;
    }
}
