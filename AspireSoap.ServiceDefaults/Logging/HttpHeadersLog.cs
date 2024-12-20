﻿using System.Text.Json;

namespace AspireSoap.ServiceDefaults.Logging;

public class HttpHeadersLog : Dictionary<string, string>
{
    private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

    public HttpHeadersLog(List<KeyValuePair<string, object?>> keyValues)
    {
        foreach (var kvp in keyValues)
        {
            if(kvp.Value?.ToString() is not null)
            {
                this.Add(kvp.Key, kvp.Value.ToString()!);
            }
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, _jsonSerializerOptions);
    }
}