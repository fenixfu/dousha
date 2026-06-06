using Dousha.Windows.Core;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DoubaoProtocolTests
{
    [Fact]
    public void UserAgentMatchesMacosDoubaoConstantForCredentialAndWebSocketRequests()
    {
        const string expected = "com.bytedance.android.doubaoime/100102018 (Linux; U; Android 16; en_US; Pixel 7 Pro; Build/BP2A.250605.031.A2; Cronet/TTNetVersion:94cf429a 2025-11-17 QuicVersion:1f89f732 2025-05-08)";

        var registration = DoubaoProtocol.BuildRegistrationRequest("cdid-1", "0011223344556677", "11111111-2222-3333-4444-555555555555", 1234);
        var token = DoubaoProtocol.BuildTokenRequest("device-1", "cdid-1", 1234);
        var webSocket = DoubaoWebSocketHandshakeHeaders.Create();

        Assert.Equal(expected, DoubaoProtocol.UserAgent);
        Assert.Equal(expected, registration.Headers["User-Agent"]);
        Assert.Equal(expected, token.Headers["User-Agent"]);
        Assert.Equal(expected, webSocket["User-Agent"]);
    }

    [Fact]
    public void RegistrationRequestMatchesMacosDerivedRequestIdentity()
    {
        var cdid = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        var openudid = "0011223344556677";
        var clientudid = "11111111-2222-3333-4444-555555555555";
        var request = DoubaoProtocol.BuildRegistrationRequest(cdid, openudid, clientudid, 1234);
        var query = Query(request.Url);
        var body = Json.Object(request.Body);
        var header = Json.Object(Json.Property(body, "header"));

        var expectedQueryKeys = new[]
        {
            "device_platform", "os", "ssmix", "_rticket", "cdid", "channel", "aid", "app_name",
            "version_code", "version_name", "manifest_version_code", "update_version_code",
            "resolution", "dpi", "device_type", "device_brand", "language", "os_api", "os_version", "ac"
        };
        var expectedHeaderKeys = new[]
        {
            "aid", "app_name", "version_code", "version_name", "manifest_version_code", "update_version_code",
            "channel", "package", "device_platform", "os", "os_api", "os_version", "device_type",
            "device_brand", "device_model", "resolution", "dpi", "language", "timezone", "access", "rom",
            "rom_version", "device_id", "install_id", "openudid", "clientudid", "cdid", "region", "tz_name",
            "tz_offset", "sim_region", "carrier_region", "cpu_abi", "build_serial", "not_request_sender",
            "sig_hash", "google_aid", "mc", "serial_number"
        };

        Assert.Equal("POST", request.Method);
        Assert.Equal("https://log.snssdk.com/service/2/device_register/", new Uri(request.Url).GetLeftPart(UriPartial.Path));
        Assert.Equal(expectedQueryKeys.Order(), query.Keys.Order());
        Assert.Equal("android", query["device_platform"]);
        Assert.Equal("android", query["os"]);
        Assert.Equal("a", query["ssmix"]);
        Assert.Equal("1234", query["_rticket"]);
        Assert.Equal(cdid, query["cdid"]);
        Assert.Equal("official", query["channel"]);
        Assert.Equal("401734", query["aid"]);
        Assert.Equal("oime", query["app_name"]);
        Assert.Equal("100102018", query["version_code"]);
        Assert.Equal("1.1.2", query["version_name"]);
        Assert.Equal("100102018", query["manifest_version_code"]);
        Assert.Equal("100102018", query["update_version_code"]);
        Assert.Equal("1080*2400", query["resolution"]);
        Assert.Equal("420", query["dpi"]);
        Assert.Equal("Pixel 7 Pro", query["device_type"]);
        Assert.Equal("google", query["device_brand"]);
        Assert.Equal("zh", query["language"]);
        Assert.Equal("34", query["os_api"]);
        Assert.Equal("16", query["os_version"]);
        Assert.Equal("wifi", query["ac"]);
        Assert.Equal(new[] { "Content-Type", "User-Agent" }.Order(), request.Headers.Keys.Order());
        Assert.Equal("application/json", request.Headers["Content-Type"]);
        Assert.Equal("ss_app_log", Json.String(Json.Property(body, "magic_tag")));
        Assert.Equal(1234, Json.Int(Json.Property(body, "_gen_time")));
        Assert.Equal(expectedHeaderKeys.Order(), header.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal(401734, Json.Int(Json.Property(header, "aid")));
        Assert.Equal("oime", Json.String(Json.Property(header, "app_name")));
        Assert.Equal(100102018, Json.Int(Json.Property(header, "version_code")));
        Assert.Equal("1.1.2", Json.String(Json.Property(header, "version_name")));
        Assert.Equal(100102018, Json.Int(Json.Property(header, "manifest_version_code")));
        Assert.Equal(100102018, Json.Int(Json.Property(header, "update_version_code")));
        Assert.Equal("official", Json.String(Json.Property(header, "channel")));
        Assert.Equal("com.bytedance.android.doubaoime", Json.String(Json.Property(header, "package")));
        Assert.Equal("android", Json.String(Json.Property(header, "device_platform")));
        Assert.Equal("android", Json.String(Json.Property(header, "os")));
        Assert.Equal("34", Json.String(Json.Property(header, "os_api")));
        Assert.Equal("16", Json.String(Json.Property(header, "os_version")));
        Assert.Equal("Pixel 7 Pro", Json.String(Json.Property(header, "device_type")));
        Assert.Equal("google", Json.String(Json.Property(header, "device_brand")));
        Assert.Equal("Pixel 7 Pro", Json.String(Json.Property(header, "device_model")));
        Assert.Equal("1080*2400", Json.String(Json.Property(header, "resolution")));
        Assert.Equal("420", Json.String(Json.Property(header, "dpi")));
        Assert.Equal("zh", Json.String(Json.Property(header, "language")));
        Assert.Equal(8, Json.Int(Json.Property(header, "timezone")));
        Assert.Equal("wifi", Json.String(Json.Property(header, "access")));
        Assert.Equal("UP1A.231005.007", Json.String(Json.Property(header, "rom")));
        Assert.Equal("UP1A.231005.007", Json.String(Json.Property(header, "rom_version")));
        Assert.Equal(0, Json.Int(Json.Property(header, "device_id")));
        Assert.Equal(0, Json.Int(Json.Property(header, "install_id")));
        Assert.Equal(openudid, Json.String(Json.Property(header, "openudid")));
        Assert.Equal(clientudid, Json.String(Json.Property(header, "clientudid")));
        Assert.Equal(cdid, Json.String(Json.Property(header, "cdid")));
        Assert.Equal("CN", Json.String(Json.Property(header, "region")));
        Assert.Equal("Asia/Shanghai", Json.String(Json.Property(header, "tz_name")));
        Assert.Equal(28800, Json.Int(Json.Property(header, "tz_offset")));
        Assert.Equal("cn", Json.String(Json.Property(header, "sim_region")));
        Assert.Equal("cn", Json.String(Json.Property(header, "carrier_region")));
        Assert.Equal("arm64-v8a", Json.String(Json.Property(header, "cpu_abi")));
        Assert.Equal("unknown", Json.String(Json.Property(header, "build_serial")));
        Assert.Equal(0, Json.Int(Json.Property(header, "not_request_sender")));
        Assert.Equal("", Json.String(Json.Property(header, "sig_hash")));
        Assert.Equal("", Json.String(Json.Property(header, "google_aid")));
        Assert.Equal("", Json.String(Json.Property(header, "mc")));
        Assert.Equal("", Json.String(Json.Property(header, "serial_number")));
        Assert.DoesNotContain("token", request.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TokenRequestMatchesMacosDerivedSettingsShape()
    {
        var request = DoubaoProtocol.BuildTokenRequest("device-1", "cdid-1", 1234);
        var query = Query(request.Url);
        var expectedQueryKeys = new[]
        {
            "device_platform", "os", "ssmix", "_rticket", "cdid", "channel", "aid", "app_name",
            "version_code", "version_name", "device_id"
        };

        Assert.Equal("POST", request.Method);
        Assert.Equal("https://is.snssdk.com/service/settings/v3/", new Uri(request.Url).GetLeftPart(UriPartial.Path));
        Assert.Equal(expectedQueryKeys.Order(), query.Keys.Order());
        Assert.Equal("android", query["device_platform"]);
        Assert.Equal("android", query["os"]);
        Assert.Equal("a", query["ssmix"]);
        Assert.Equal("1234", query["_rticket"]);
        Assert.Equal("cdid-1", query["cdid"]);
        Assert.Equal("official", query["channel"]);
        Assert.Equal("401734", query["aid"]);
        Assert.Equal("oime", query["app_name"]);
        Assert.Equal("100102018", query["version_code"]);
        Assert.Equal("1.1.2", query["version_name"]);
        Assert.Equal("device-1", query["device_id"]);
        Assert.Equal(new[] { "Content-Type", "User-Agent", "x-ss-stub" }.Order(), request.Headers.Keys.Order());
        Assert.Equal("application/x-www-form-urlencoded", request.Headers["Content-Type"]);
        Assert.Equal(DoubaoProtocol.UserAgent, request.Headers["User-Agent"]);
        Assert.Equal("46C03B52742B3F2615A3ABDF1636B754", request.Headers["x-ss-stub"]);
        Assert.Equal("body=null", request.Body);
    }

    [Fact]
    public void RegistrationResponseAcceptsStringAndNumericDeviceIdentifiers()
    {
        var stringIds = DoubaoProtocol.ParseRegistrationResponse(
            "{\"device_id_str\":\"device-string\",\"install_id_str\":\"install-string\"}",
            "cdid-1",
            "open-1",
            "client-1");
        var numericIds = DoubaoProtocol.ParseRegistrationResponse(
            "{\"device_id\":123,\"install_id\":456}",
            "cdid-2",
            "open-2",
            "client-2");

        Assert.Equal("device-string", stringIds.DeviceId);
        Assert.Equal("install-string", stringIds.InstallId);
        Assert.Equal("123", numericIds.DeviceId);
        Assert.Equal("456", numericIds.InstallId);
    }

    [Fact]
    public void TokenResponseReadsAppKeyFromAsrConfig()
    {
        var token = DoubaoProtocol.ParseTokenResponse(
            "{\"data\":{\"settings\":{\"asr_config\":{\"app_key\":\"jwt-token\"}}}}");

        Assert.Equal("jwt-token", token);
    }

    [Fact]
    public void SessionConfigMatchesMacosOfficialProfileJson()
    {
        var actual = DoubaoProtocol.BuildSessionConfigJson("device-1", contextHint: "domain words");
        var expected = """
        {
          "audio_info": {
            "channel": 1,
            "format": "speech_opus",
            "sample_rate": 16000
          },
          "enable_punctuation": true,
          "enable_speech_rejection": false,
          "extra": {
            "app_name": "com.android.chrome",
            "app_version": "1.1.2",
            "cell_compress_rate": 8,
            "context": "domain words",
            "device_brand": "google",
            "device_model": "Pixel 7 Pro",
            "did": "device-1",
            "enable_asr_threepass": true,
            "enable_asr_twopass": true,
            "enable_print_chinese": false,
            "end_smooth_window_ms": 800,
            "input_mode": "tool",
            "os": "Android",
            "os_version": "16",
            "remove_space_between_han_eng": false,
            "remove_space_between_han_num": false,
            "strong_ddc": true,
            "use_twopass_retry": true
          }
        }
        """;

        Assert.Equal(CanonicalJson(expected), CanonicalJson(actual));
        Assert.DoesNotContain("\"language\"", actual);
    }

    [Fact]
    public void RecognitionMessagesRoundTripThroughAsrRequestDecoder()
    {
        var startTask = DoubaoAsrMessageBuilder.StartTask("request-1", "token-1");
        var startSession = DoubaoAsrMessageBuilder.StartSession("request-1", "token-1", "{\"config\":true}");
        var recognition = DoubaoAsrMessageBuilder.RecognitionFrame("request-1", [1, 2, 3], FrameState.Last, timestampMs: 42);
        var finish = DoubaoAsrMessageBuilder.FinishSession("request-1", "token-1");

        Assert.Equal("StartTask", DoubaoAsrRequest.Decode(startTask).MethodName);
        Assert.Equal("{\"config\":true}", DoubaoAsrRequest.Decode(startSession).Payload);
        var decodedRecognition = DoubaoAsrRequest.Decode(recognition);
        Assert.Equal("TaskRequest", decodedRecognition.MethodName);
        Assert.Equal([1, 2, 3], decodedRecognition.AudioData.ToArray());
        Assert.Equal(FrameState.Last, decodedRecognition.FrameState);
        Assert.Contains("\"finish_audio\":true", decodedRecognition.Payload);
        Assert.Equal("FinishSession", DoubaoAsrRequest.Decode(finish).MethodName);
    }

    [Fact]
    public void ResponseParserParsesControlHeartbeatAndRecognitionFixturesWithoutLeakingTranscriptToDiagnostics()
    {
        var logger = new RecordingDiagnosticLog();
        var parser = new DoubaoAsrResponseParser(logger);
        var control = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "SessionStarted",
            StatusCode: 20000000,
            StatusMessage: "ok",
            ResultJson: ""));
        var heartbeat = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "TaskResponse",
            StatusCode: 20000000,
            StatusMessage: "ok",
            ResultJson: "{\"results\":[]}"));
        var recognition = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "TaskResponse",
            StatusCode: 20000000,
            StatusMessage: "ok",
            ResultJson: "{\"results\":[{\"text\":\"旧文本\",\"is_interim\":true,\"is_vad_finished\":false},{\"text\":\"完整听写文本\",\"is_interim\":false,\"is_vad_finished\":true,\"extra\":{\"nonstream_result\":false}}]}"));

        var controlEvent = parser.Parse(control);
        var heartbeatEvent = parser.Parse(heartbeat);
        var recognitionEvent = parser.Parse(recognition);

        Assert.Equal("SessionStarted", controlEvent.MessageType);
        Assert.Null(controlEvent.Text);
        Assert.True(heartbeatEvent.IsHeartbeat);
        Assert.Null(heartbeatEvent.Text);
        Assert.Equal("完整听写文本", recognitionEvent.Text);
        Assert.False(recognitionEvent.IsInterim);
        Assert.True(recognitionEvent.IsVadFinished);
        Assert.True(recognitionEvent.IsFinalized);
        Assert.DoesNotContain("完整听写文本", logger.Joined);
        Assert.DoesNotContain("旧文本", logger.Joined);
        Assert.Contains("resultJsonLength=", logger.Joined);
    }

    [Fact]
    public void ResponseParserTreatsNonstreamResultAsFinalized()
    {
        var parser = new DoubaoAsrResponseParser(new RecordingDiagnosticLog());
        var response = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "TaskResponse",
            StatusCode: 20000000,
            StatusMessage: "ok",
            ResultJson: "{\"results\":[{\"text\":\"非流式结果\",\"is_interim\":true,\"is_vad_finished\":false,\"extra\":{\"nonstream_result\":true}}]}"));

        var parsed = parser.Parse(response);

        Assert.Equal("非流式结果", parsed.Text);
        Assert.True(parsed.IsFinalized);
    }

    [Fact]
    public void ResponseParserTreatsFailedEmptySessionResponseAsTerminalProtocolFailure()
    {
        var logger = new RecordingDiagnosticLog();
        var parser = new DoubaoAsrResponseParser(logger);
        var response = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "SessionFailed",
            StatusCode: 40000000,
            StatusMessage: "",
            ResultJson: ""));

        var exception = Assert.Throws<DoubaoProtocolException>(() => parser.Parse(response, "StartTask"));

        Assert.Equal("SessionFailed", exception.MessageType);
        Assert.Equal(40000000, exception.StatusCode);
        Assert.Equal(0, exception.StatusMessageLength);
        Assert.Equal("StartTask", exception.Phase);
        Assert.Contains("doubao.protocol.failure messageType=SessionFailed statusCode=40000000 statusMessageLength=0 phase=StartTask", logger.Joined);
        Assert.DoesNotContain("request-1", exception.Message);
    }

    [Theory]
    [InlineData("TaskFailed")]
    [InlineData("SessionFailed")]
    public void ResponseParserTreatsFailedMessageTypeAsTerminalRegardlessOfStatusCode(string messageType)
    {
        var parser = new DoubaoAsrResponseParser(new RecordingDiagnosticLog());
        var response = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: messageType,
            StatusCode: 20000000,
            StatusMessage: "ok",
            ResultJson: ""));

        var exception = Assert.Throws<DoubaoProtocolException>(() => parser.Parse(response, "control"));

        Assert.Equal(messageType, exception.MessageType);
        Assert.Equal(20000000, exception.StatusCode);
        Assert.Equal("terminal", exception.Reason);
    }

    [Fact]
    public void ResponseParserTreatsLegacyHttpSuccessStatusAsTerminal()
    {
        var parser = new DoubaoAsrResponseParser(new RecordingDiagnosticLog());
        var response = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "TaskStarted",
            StatusCode: 200,
            StatusMessage: "ok",
            ResultJson: ""));

        var exception = Assert.Throws<DoubaoProtocolException>(() => parser.ParseControl(
            response,
            "StartTask",
            expectedMessageType: "TaskStarted",
            expectedRequestId: "request-1"));

        Assert.Equal("TaskStarted", exception.MessageType);
        Assert.Equal(200, exception.StatusCode);
        Assert.Equal("terminal", exception.Reason);
    }

    [Fact]
    public void ResponseParserTreatsNonSuccessStatusAsTerminalProtocolFailure()
    {
        var logger = new RecordingDiagnosticLog();
        var parser = new DoubaoAsrResponseParser(logger);
        const string rawStatusMessage = "Opus audio frame decode failed for token-secret and transcript words";
        var response = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "TaskStarted",
            StatusCode: 500,
            StatusMessage: rawStatusMessage,
            ResultJson: ""));

        var exception = Assert.Throws<DoubaoProtocolException>(() => parser.Parse(response, "StartTask"));

        Assert.Equal("TaskStarted", exception.MessageType);
        Assert.Equal(500, exception.StatusCode);
        Assert.Equal(rawStatusMessage.Length, exception.StatusMessageLength);
        Assert.Equal("StartTask", exception.Phase);
        Assert.DoesNotContain(rawStatusMessage, logger.Joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(rawStatusMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token-secret", logger.Joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token-secret", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> Query(string url)
    {
        var uri = new Uri(url);
        return uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                pair => WebUtility.UrlDecode(pair[0]),
                pair => pair.Length == 2 ? WebUtility.UrlDecode(pair[1]) : "");
    }

    private static string CanonicalJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return Canonicalize(document.RootElement);
    }

    private static string Canonicalize(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => "{" + string.Join(",", element.EnumerateObject()
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => JsonSerializer.Serialize(property.Name) + ":" + Canonicalize(property.Value))) + "}",
            JsonValueKind.Array => "[" + string.Join(",", element.EnumerateArray().Select(Canonicalize)) + "]",
            _ => element.GetRawText()
        };
    }
}
