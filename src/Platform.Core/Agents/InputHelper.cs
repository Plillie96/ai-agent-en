using System.Text.Json;

namespace Platform.Core.Agents;

public static class InputHelper
{
    public static int GetInt(this Dictionary<string, object> dict, string key, int defaultValue = 0)
    {
        if (!dict.TryGetValue(key, out var val)) return defaultValue;
        if (val is JsonElement je) return je.ValueKind == JsonValueKind.Number ? je.GetInt32() : (int.TryParse(je.GetRawText(), out var p) ? p : defaultValue);
        return Convert.ToInt32(val);
    }

    public static double GetDouble(this Dictionary<string, object> dict, string key, double defaultValue = 0.0)
    {
        if (!dict.TryGetValue(key, out var val)) return defaultValue;
        if (val is JsonElement je) return je.ValueKind == JsonValueKind.Number ? je.GetDouble() : (double.TryParse(je.GetRawText(), out var p) ? p : defaultValue);
        return Convert.ToDouble(val);
    }

    public static decimal GetDecimal(this Dictionary<string, object> dict, string key, decimal defaultValue = 0m)
    {
        if (!dict.TryGetValue(key, out var val)) return defaultValue;
        if (val is JsonElement je) return je.ValueKind == JsonValueKind.Number ? je.GetDecimal() : (decimal.TryParse(je.GetRawText(), out var p) ? p : defaultValue);
        return Convert.ToDecimal(val);
    }

    public static string GetString(this Dictionary<string, object> dict, string key, string defaultValue = "")
    {
        if (!dict.TryGetValue(key, out var val)) return defaultValue;
        if (val is JsonElement je) return je.ToString();
        return val?.ToString() ?? defaultValue;
    }
}