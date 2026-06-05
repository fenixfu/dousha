using System.Text.Json;

namespace Dousha.Windows.Tests;

internal static class Json
{
    public static JsonElement Object(string json)
    {
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    public static JsonElement Object(JsonElement element)
    {
        return element;
    }

    public static JsonElement Property(JsonElement element, string name)
    {
        return element.GetProperty(name);
    }

    public static string String(JsonElement element)
    {
        return element.GetString() ?? "";
    }

    public static int Int(JsonElement element)
    {
        return element.GetInt32();
    }
}
