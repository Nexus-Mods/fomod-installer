using FomodInstaller.Interface;

using System.Collections.Generic;

namespace ModInstaller.Lite;

public record InstallResult
{
    public string Message { get; set; }
    public required List<Instruction> Instructions { get; set; }
}