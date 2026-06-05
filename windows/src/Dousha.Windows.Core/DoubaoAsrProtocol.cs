using System.Text;
using System.Text.Json;

namespace Dousha.Windows.Core;

public enum FrameState
{
    Unspecified = 0,
    First = 1,
    Middle = 3,
    Last = 9
}

public sealed record DoubaoAsrRequest(
    string Token,
    string ServiceName,
    string MethodName,
    string Payload,
    ReadOnlyMemory<byte> AudioData,
    string RequestId,
    FrameState FrameState)
{
    public static DoubaoAsrRequest Decode(ReadOnlyMemory<byte> data)
    {
        var fields = DoubaoWire.DecodeFields(data.Span);
        return new DoubaoAsrRequest(
            StringField(fields, 2),
            StringField(fields, 3),
            StringField(fields, 5),
            StringField(fields, 6),
            BytesField(fields, 7),
            StringField(fields, 8),
            (FrameState)IntField(fields, 9));
    }

    private static string StringField(Dictionary<int, DoubaoWire.Field> fields, int tag)
    {
        return fields.TryGetValue(tag, out var field) && field.Bytes is not null
            ? Encoding.UTF8.GetString(field.Bytes)
            : "";
    }

    private static ReadOnlyMemory<byte> BytesField(Dictionary<int, DoubaoWire.Field> fields, int tag)
    {
        return fields.TryGetValue(tag, out var field) && field.Bytes is not null ? field.Bytes : Array.Empty<byte>();
    }

    private static int IntField(Dictionary<int, DoubaoWire.Field> fields, int tag)
    {
        return fields.TryGetValue(tag, out var field) ? (int)field.Varint : 0;
    }
}

public sealed record DoubaoAsrResponse(
    string RequestId,
    string MessageType,
    int StatusCode,
    string StatusMessage,
    string ResultJson)
{
    public static byte[] Encode(DoubaoAsrResponse response)
    {
        var writer = new DoubaoWire.Writer();
        writer.String(1, response.RequestId);
        writer.String(4, response.MessageType);
        writer.Int32(5, response.StatusCode);
        writer.String(6, response.StatusMessage);
        writer.String(7, response.ResultJson);
        return writer.ToArray();
    }

    public static DoubaoAsrResponse Decode(ReadOnlyMemory<byte> data)
    {
        var fields = DoubaoWire.DecodeFields(data.Span);
        return new DoubaoAsrResponse(
            StringField(fields, 1),
            StringField(fields, 4),
            IntField(fields, 5),
            StringField(fields, 6),
            StringField(fields, 7));
    }

    private static string StringField(Dictionary<int, DoubaoWire.Field> fields, int tag)
    {
        return fields.TryGetValue(tag, out var field) && field.Bytes is not null
            ? Encoding.UTF8.GetString(field.Bytes)
            : "";
    }

    private static int IntField(Dictionary<int, DoubaoWire.Field> fields, int tag)
    {
        return fields.TryGetValue(tag, out var field) ? (int)field.Varint : 0;
    }
}

public static class DoubaoAsrMessageBuilder
{
    public static byte[] StartTask(string requestId, string token)
    {
        return Request(token, "StartTask", "", [], requestId, FrameState.Unspecified);
    }

    public static byte[] StartSession(string requestId, string token, string configJson)
    {
        return Request(token, "StartSession", configJson, [], requestId, FrameState.Unspecified);
    }

    public static byte[] FinishSession(string requestId, string token)
    {
        return Request(token, "FinishSession", "", [], requestId, FrameState.Unspecified);
    }

    public static byte[] RecognitionFrame(string requestId, byte[] opusAudio, FrameState frameState, long timestampMs)
    {
        var extra = frameState == FrameState.Last ? "{\"finish_audio\":true}" : "{}";
        var payload = $"{{\"extra\":{extra},\"timestamp_ms\":{timestampMs}}}";
        return Request("", "TaskRequest", payload, opusAudio, requestId, frameState);
    }

    private static byte[] Request(string token, string methodName, string payload, byte[] audioData, string requestId, FrameState frameState)
    {
        var writer = new DoubaoWire.Writer();
        writer.String(2, token);
        writer.String(3, "ASR");
        writer.String(5, methodName);
        writer.String(6, payload);
        writer.Bytes(7, audioData);
        writer.String(8, requestId);
        writer.Int32(9, (int)frameState);
        return writer.ToArray();
    }
}

public sealed record DoubaoRecognitionEvent(
    string MessageType,
    int StatusCode,
    string? Text,
    bool IsInterim,
    bool IsVadFinished,
    bool IsFinalized,
    bool IsHeartbeat);

public sealed class DoubaoProtocolException : Exception
{
    public DoubaoProtocolException(string messageType, int statusCode, int statusMessageLength, string phase)
        : base($"Doubao protocol failure phase={phase} messageType={messageType} statusCode={statusCode} statusMessageLength={statusMessageLength}")
    {
        MessageType = messageType;
        StatusCode = statusCode;
        StatusMessageLength = statusMessageLength;
        Phase = phase;
    }

    public string MessageType { get; }

    public int StatusCode { get; }

    public int StatusMessageLength { get; }

    public string Phase { get; }
}

public sealed class DoubaoAsrResponseParser
{
    private readonly IDiagnosticLog _diagnosticLog;

    public DoubaoAsrResponseParser(IDiagnosticLog diagnosticLog)
    {
        _diagnosticLog = diagnosticLog;
    }

    public DoubaoRecognitionEvent Parse(ReadOnlyMemory<byte> data, string phase = "unknown")
    {
        var response = DoubaoAsrResponse.Decode(data);
        _diagnosticLog.Lifecycle($"doubao.protocol.response messageType={response.MessageType} statusCode={response.StatusCode} resultJsonLength={response.ResultJson.Length}");
        if (response.StatusCode != 200 || response.MessageType.EndsWith("Failed", StringComparison.OrdinalIgnoreCase))
        {
            var statusMessageLength = response.StatusMessage.Length;
            _diagnosticLog.Lifecycle($"doubao.protocol.failure messageType={response.MessageType} statusCode={response.StatusCode} statusMessageLength={statusMessageLength} phase={phase}");
            throw new DoubaoProtocolException(response.MessageType, response.StatusCode, statusMessageLength, phase);
        }

        if (string.IsNullOrWhiteSpace(response.ResultJson))
        {
            return new DoubaoRecognitionEvent(response.MessageType, response.StatusCode, null, true, false, false, false);
        }

        using var doc = JsonDocument.Parse(response.ResultJson);
        if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind is not JsonValueKind.Array)
        {
            return new DoubaoRecognitionEvent(response.MessageType, response.StatusCode, null, true, false, false, true);
        }

        if (results.GetArrayLength() == 0)
        {
            return new DoubaoRecognitionEvent(response.MessageType, response.StatusCode, null, true, false, false, true);
        }

        string? text = null;
        var isInterim = true;
        var vadFinished = false;
        var nonstreamResult = false;
        foreach (var result in results.EnumerateArray())
        {
            if (result.TryGetProperty("text", out var textElement) && !string.IsNullOrEmpty(textElement.GetString()))
            {
                text = textElement.GetString();
            }

            if (result.TryGetProperty("is_interim", out var interimElement) && interimElement.ValueKind is JsonValueKind.False)
            {
                isInterim = false;
            }

            if (result.TryGetProperty("is_vad_finished", out var vadElement) && vadElement.ValueKind is JsonValueKind.True)
            {
                vadFinished = true;
            }

            if (result.TryGetProperty("extra", out var extra)
                && extra.ValueKind is JsonValueKind.Object
                && extra.TryGetProperty("nonstream_result", out var nonstreamElement)
                && nonstreamElement.ValueKind is JsonValueKind.True)
            {
                nonstreamResult = true;
            }
        }

        if (text is not null)
        {
            _diagnosticLog.TranscriptReceived(text);
        }

        return new DoubaoRecognitionEvent(
            response.MessageType,
            response.StatusCode,
            text,
            isInterim,
            vadFinished,
            (!isInterim && vadFinished) || nonstreamResult,
            text is null);
    }
}
