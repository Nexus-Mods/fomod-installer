using System.Collections.Generic;
using FomodInstaller.Interface;

namespace ModInstaller.Lite;

public record InstallResult
{
    public string Message { get; set; }
    public required List<Instruction> Instructions { get; set; }
}