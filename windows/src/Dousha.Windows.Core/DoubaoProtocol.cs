using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dousha.Windows.Core;

public static class DoubaoProtocol
{
    public const int Aid = 401734;
    public const string UserAgent = "com.bytedance.android.doubaoime/100102018 (Linux; U; Android 16; en_US; Pixel 7 Pro; Build/BP2A.250605.031.A2; Cronet/TTNetVersion:94cf429a 2025-11-17 QuicVersion:1f89f732 2025-05-08)";
    public const string WebSocketEndpoint = "wss://frontier-audio-ime-ws.doubao.com/ocean/api/v1/ws";

    public static DoubaoHttpRequest BuildRegistrationRequest(string cdid, string openudid, string clientudid, long ticket)
    {
        var url = BuildUrl(
            "https://log.snssdk.com/service/2/device_register/",
            [
                ("device_platform", "android"),
                ("os", "android"),
                ("ssmix", "a"),
                ("_rticket", ticket.ToString()),
                ("cdid", cdid),
                ("channel", "official"),
                ("aid", Aid.ToString()),
                ("app_name", "oime"),
                ("version_code", "100102018"),
                ("version_name", "1.1.2"),
                ("manifest_version_code", "100102018"),
                ("update_version_code", "100102018"),
                ("resolution", "1080*2400"),
                ("dpi", "420"),
                ("device_type", "Pixel 7 Pro"),
                ("device_brand", "google"),
                ("language", "zh"),
                ("os_api", "34"),
                ("os_version", "16"),
                ("ac", "wifi")
            ]);
        var body = JsonSerializer.Serialize(new
        {
            magic_tag = "ss_app_log",
            header = new Dictionary<string, object?>
            {
                ["aid"] = Aid,
                ["app_name"] = "oime",
                ["version_code"] = 100102018,
                ["version_name"] = "1.1.2",
                ["manifest_version_code"] = 100102018,
                ["update_version_code"] = 100102018,
                ["channel"] = "official",
                ["package"] = "com.bytedance.android.doubaoime",
                ["device_platform"] = "android",
                ["os"] = "android",
                ["os_api"] = "34",
                ["os_version"] = "16",
                ["device_type"] = "Pixel 7 Pro",
                ["device_brand"] = "google",
                ["device_model"] = "Pixel 7 Pro",
                ["resolution"] = "1080*2400",
                ["dpi"] = "420",
                ["language"] = "zh",
                ["timezone"] = 8,
                ["access"] = "wifi",
                ["rom"] = "UP1A.231005.007",
                ["rom_version"] = "UP1A.231005.007",
                ["device_id"] = 0,
                ["install_id"] = 0,
                ["openudid"] = openudid,
                ["clientudid"] = clientudid,
                ["cdid"] = cdid,
                ["region"] = "CN",
                ["tz_name"] = "Asia/Shanghai",
                ["tz_offset"] = 28800,
                ["sim_region"] = "cn",
                ["carrier_region"] = "cn",
                ["cpu_abi"] = "arm64-v8a",
                ["build_serial"] = "unknown",
                ["not_request_sender"] = 0,
                ["sig_hash"] = "",
                ["google_aid"] = "",
                ["mc"] = "",
                ["serial_number"] = ""
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
        var url = BuildUrl(
            "https://is.snssdk.com/service/settings/v3/",
            [
                ("device_platform", "android"),
                ("os", "android"),
                ("ssmix", "a"),
                ("_rticket", ticket.ToString()),
                ("cdid", cdid),
                ("channel", "official"),
                ("aid", Aid.ToString()),
                ("app_name", "oime"),
                ("version_code", "100102018"),
                ("version_name", "1.1.2"),
                ("device_id", deviceId)
            ]);
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
                ["app_name"] = "com.android.chrome",
                ["app_version"] = "1.1.2",
                ["cell_compress_rate"] = 8,
                ["device_brand"] = "google",
                ["device_model"] = "Pixel 7 Pro",
                ["did"] = deviceId,
                ["enable_asr_threepass"] = true,
                ["enable_asr_twopass"] = true,
                ["enable_print_chinese"] = false,
                ["end_smooth_window_ms"] = 800,
                ["input_mode"] = "tool",
                ["os"] = "Android",
                ["os_version"] = "16",
                ["remove_space_between_han_eng"] = false,
                ["remove_space_between_han_num"] = false,
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

    private static string BuildUrl(string baseUrl, IEnumerable<(string Name, string Value)> query)
    {
        return baseUrl + "?" + string.Join("&", query.Select(item =>
            $"{Uri.EscapeDataString(item.Name)}={Uri.EscapeDataString(item.Value)}"));
    }
}
