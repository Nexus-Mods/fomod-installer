using FomodInstaller.Interface;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace TestData;

public record InstallInstruction
{
    public string type { get; set; }
    public string source { get; set; }
    public string destination { get; set; }
    public string section { get; set; }
    public string key { get; set; }
    public string value { get; set; }
    public JsonDocument data { get; set; }
    public int priority { get; set; }
}

public static class InstallInstructionExtensions
{
    public static IEnumerable<Instruction> ToInstruction(this IEnumerable<InstallInstruction> q) => q.Select(x => new Instruction
    {
        type = x.type,
        source = x.source,
        destination = x.destination,
        section = x.section,
        key = x.key,
        value = x.value,
        data = ConvertJsonDocumentToByteArray(x.data),
        priority = x.priority,
    });

    private static byte[] ConvertJsonDocumentToByteArray(JsonDocument doc)
    {
        if (doc == null)
            return null;

        var root = doc.RootElement;

        // Handle Node.js Buffer format: {"type": "Buffer", "data": "base64string"}
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("type", out var typeElement) &&
            typeElement.GetString() == "Buffer" &&
            root.TryGetProperty("data", out var dataElement))
        {
            if (dataElement.ValueKind == JsonValueKind.String)
            {
                // Base64 encoded data
                return Convert.FromBase64String(dataElement.GetString());
            }
            else if (dataElement.ValueKind == JsonValueKind.Array)
            {
                // Array of bytes
                return dataElement.EnumerateArray().Select(e => (byte)e.GetInt32()).ToArray();
            }
        }

        return null;
    }
}