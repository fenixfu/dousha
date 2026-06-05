using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dousha.Windows.Core;

public static class DoubaoProtocol
{
    public const int Aid = 401734;
    public const string UserAgent = "com.bytedance.android.doubaoime/100102018 (Linux; U; Android 16; en_US; Pixel 7 Pro)";

    public static DoubaoHttpRequest BuildRegistrationRequest(string cdid, string openudid, string clientudid, long ticket)
    {
        var url = "https://log.snssdk.com/service/2/device_register/"
            + $"?device_platform=android&os=android&ssmix=a&_rticket={ticket}&cdid={Uri.EscapeDataString(cdid)}"
            + $"&channel=official&aid={Aid}&app_name=oime&version_code=100102018&version_name=1.1.2";
        var body = JsonSerializer.Serialize(new
        {
            magic_tag = "ss_app_log",
            header = new Dictionary<string, object?>
            {
                ["aid"] = Aid,
                ["app_name"] = "oime",
                ["device_id"] = 0,
                ["install_id"] = 0,
                ["openudid"] = openudid,
                ["clientudid"] = clientudid,
                ["cdid"] = cdid,
                ["region"] = "CN",
                ["tz_name"] = "Asia/Shanghai",
                ["device_platform"] = "android",
                ["device_type"] = "Pixel 7 Pro",
                ["device_brand"] = "google",
                ["language"] = "zh"
            },
            _gen_time = ticket
        });

        return new DoubaoHttpRequest(
            "POST",
            url,
            new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["User-Agent"] = UserAgent
            },
            body);
    }

    public static DoubaoHttpRequest BuildTokenRequest(string deviceId, string cdid, long ticket)
    {
        const string body = "body=null";
        var url = "https://is.snssdk.com/service/settings/v3/"
            + $"?device_platform=android&os=android&ssmix=a&_rticket={ticket}&cdid={Uri.EscapeDataString(cdid)}"
            + $"&channel=official&aid={Aid}&app_name=oime&version_code=100102018&version_name=1.1.2&device_id={Uri.EscapeDataString(deviceId)}";
        return new DoubaoHttpRequest(
            "POST",
            url,
            new Dictionary<string, string>
            {
                ["Content-Type"] = "application/x-www-form-urlencoded",
                ["User-Agent"] = UserAgent,
                ["x-ss-stub"] = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(body)))
            },
            body);
    }

    public static string BuildSessionConfigJson(string deviceId, string contextHint)
    {
        var payload = new Dictionary<string, object?>
        {
            ["audio_info"] = new Dictionary<string, object?>
            {
                ["channel"] = 1,
                ["format"] = "speech_opus",
                ["sample_rate"] = 16000
            },
            ["enable_punctuation"] = true,
            ["enable_speech_rejection"] = false,
            ["extra"] = new Dictionary<string, object?>
            {
                ["context"] = contextHint,
                ["device_brand"] = "google",
                ["device_model"] = "Pixel 7 Pro",
                ["did"] = deviceId,
                ["enable_asr_threepass"] = true,
                ["enable_asr_twopass"] = true,
                ["end_smooth_window_ms"] = 800,
                ["input_mode"] = "tool",
                ["language"] = "zh",
                ["os"] = "Android",
                ["os_version"] = "16",
                ["strong_ddc"] = true,
                ["use_twopass_retry"] = true
            }
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
    }

    public static RegisteredDoubaoDevice ParseRegistrationResponse(
        string json,
        string cdid,
        string openudid,
        string clientudid)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var deviceId = StringOrNumber(root, "device_id_str", "device_id");
        if (string.IsNullOrWhiteSpace(deviceId) || deviceId == "0")
        {
            throw new InvalidOperationException("Doubao registration response did not include a valid device id.");
        }

        var installId = StringOrNumber(root, "install_id_str", "install_id");
        return new RegisteredDoubaoDevice(deviceId, installId, cdid, openudid, clientudid);
    }

    public static string ParseTokenResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("data", out var data)
            && data.TryGetProperty("settings", out var settings)
            && settings.TryGetProperty("asr_config", out var asrConfig)
            && asrConfig.TryGetProperty("app_key", out var appKey)
            && !string.IsNullOrWhiteSpace(appKey.GetString()))
        {
            return appKey.GetString()!;
        }

        throw new InvalidOperationException("Doubao token response did not include data.settings.asr_config.app_key.");
    }

    private static string StringOrNumber(JsonElement root, string stringProperty, string numberProperty)
    {
        if (root.TryGetProperty(stringProperty, out var stringElement)
            && stringElement.ValueKind is JsonValueKind.String
            && !string.IsNullOrWhiteSpace(stringElement.GetString()))
        {
            return stringElement.GetString()!;
        }

        if (root.TryGetProperty(numberProperty, out var numberElement)
            && numberElement.ValueKind is JsonValueKind.Number
            && numberElement.TryGetInt64(out var number))
        {
            return number.ToString();
        }

        return "";
    }
}
