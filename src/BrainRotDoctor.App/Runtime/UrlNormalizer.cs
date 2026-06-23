namespace BrainRotDoctor.App.Runtime;

internal static class UrlNormalizer
{
    public static bool TryNormalize(string? value, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string text = value.Trim();
        if (text.Contains(' ') || text.Contains('\n') || text.Contains('\r'))
        {
            return false;
        }

        if (!text.Contains("://", StringComparison.Ordinal))
        {
            if (!LooksLikeHostPath(text))
            {
                return false;
            }

            text = "https://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? parsed))
        {
            return false;
        }

        if (parsed.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(parsed.Host))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private static bool LooksLikeHostPath(string text)
    {
        int slash = text.IndexOf('/');
        string host = slash >= 0 ? text[..slash] : text;
        return host.Contains('.', StringComparison.Ordinal)
            && host.All(c => char.IsLetterOrDigit(c) || c is '.' or '-');
    }
}
