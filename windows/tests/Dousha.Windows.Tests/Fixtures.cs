using System.Text;
using System.Text.Json;
using Dousha.Windows.Core;

namespace Dousha.Windows.Tests;

internal static class Fixtures
{
    public static DoubaoDeviceCredentials Credentials(string? token = null)
    {
        return new DoubaoDeviceCredentials(
            DeviceId: "device-cached",
            InstallId: "install-cached",
            Cdid: "cdid-cached",
            Openudid: "open-cached",
            Clientudid: "client-cached",
            Token: token ?? JwtExpiringAt(DateTimeOffset.UtcNow.AddHours(2)));
    }

    public static string JwtExpiringAt(DateTimeOffset expiresAt)
    {
        return $"{Base64Url(new { alg = "none", typ = "JWT" })}.{Base64Url(new { exp = expiresAt.ToUnixTimeSeconds() })}.signature";
    }

    private static string Base64Url(object value)
    {
        var json = JsonSerializer.Serialize(value);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
