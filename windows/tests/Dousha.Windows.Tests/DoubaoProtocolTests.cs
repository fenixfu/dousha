using Dousha.Windows.Core;
using Xunit;

namespace Dousha.Windows.Tests;

public sealed class DoubaoProtocolTests
{
    [Fact]
    public void RegistrationRequestMatchesAndroidDeviceRegistrationShape()
    {
        var request = DoubaoProtocol.BuildRegistrationRequest("cdid-1", "open-1", "client-1", 1234);

        Assert.Equal("POST", request.Method);
        Assert.Contains("device_register", request.Url);
        Assert.Contains("cdid=cdid-1", request.Url);
        Assert.Equal("application/json", request.Headers["Content-Type"]);
        Assert.Contains("\"magic_tag\":\"ss_app_log\"", request.Body);
        Assert.Contains("\"device_id\":0", request.Body);
        Assert.DoesNotContain("token", request.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TokenRequestUsesDeviceIdAndSettingsShape()
    {
        var request = DoubaoProtocol.BuildTokenRequest("device-1", "cdid-1", 1234);

        Assert.Equal("POST", request.Method);
        Assert.Contains("settings/v3", request.Url);
        Assert.Contains("device_id=device-1", request.Url);
        Assert.Equal("application/x-www-form-urlencoded", request.Headers["Content-Type"]);
        Assert.Equal("body=null", request.Body);
        Assert.True(request.Headers.ContainsKey("x-ss-stub"));
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
    public void SessionConfigIncludesDeviceIdAndMandarinFirstAudioShape()
    {
        var json = DoubaoProtocol.BuildSessionConfigJson("device-1", contextHint: "");
        var obj = Json.Object(json);
        var audioInfo = Json.Object(Json.Property(obj, "audio_info"));
        var extra = Json.Object(Json.Property(obj, "extra"));

        Assert.Equal("speech_opus", Json.String(Json.Property(audioInfo, "format")));
        Assert.Equal(16000, Json.Int(Json.Property(audioInfo, "sample_rate")));
        Assert.Equal(1, Json.Int(Json.Property(audioInfo, "channel")));
        Assert.Equal("device-1", Json.String(Json.Property(extra, "did")));
        Assert.Equal("", Json.String(Json.Property(extra, "context")));
        Assert.Equal("tool", Json.String(Json.Property(extra, "input_mode")));
        Assert.Equal("zh", Json.String(Json.Property(extra, "language")));
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
            StatusCode: 200,
            StatusMessage: "ok",
            ResultJson: ""));
        var heartbeat = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "TaskResponse",
            StatusCode: 200,
            StatusMessage: "ok",
            ResultJson: "{\"results\":[]}"));
        var recognition = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "TaskResponse",
            StatusCode: 200,
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
            StatusCode: 200,
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

    [Fact]
    public void ResponseParserTreatsNonSuccessStatusAsTerminalProtocolFailure()
    {
        var parser = new DoubaoAsrResponseParser(new RecordingDiagnosticLog());
        var response = DoubaoAsrResponse.Encode(new DoubaoAsrResponse(
            RequestId: "request-1",
            MessageType: "TaskStarted",
            StatusCode: 500,
            StatusMessage: "server detail",
            ResultJson: ""));

        var exception = Assert.Throws<DoubaoProtocolException>(() => parser.Parse(response, "StartTask"));

        Assert.Equal("TaskStarted", exception.MessageType);
        Assert.Equal(500, exception.StatusCode);
        Assert.Equal(13, exception.StatusMessageLength);
        Assert.Equal("StartTask", exception.Phase);
        Assert.DoesNotContain("server detail", exception.Message);
    }
}
