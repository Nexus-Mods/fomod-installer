using FomodInstaller.Interface;

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
        //data = x.Data,
        priority = x.priority,
    });
}