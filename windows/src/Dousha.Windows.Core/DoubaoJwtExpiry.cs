using System.Text;
using System.Text.Json;

namespace Dousha.Windows.Core;

public static class DoubaoJwtExpiry
{
    public static bool IsExpired(string token, DateTimeOffset now, int marginSeconds = 60)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        try
        {
            var payload = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("exp", out var exp) || !exp.TryGetInt64(out var expSeconds))
            {
                return false;
            }

            return now.ToUnixTimeSeconds() >= expSeconds - marginSeconds;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        while (padded.Length % 4 != 0)
        {
            padded += "=";
        }

        return Convert.FromBase64String(padded);
    }
}
