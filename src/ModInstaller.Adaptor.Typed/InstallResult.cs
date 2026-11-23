using FomodInstaller.Interface;
using FomodInstaller.Interface.snapshot;

using System.Collections.Generic;

namespace ModInstaller.Lite;

public record InstallResult
{
    public string Message { get; set; }
    public required List<Instruction> Instructions { get; set; }

    /// <summary>
    /// A structured snapshot of every option the installer considered, with the type each one
    /// resolved to, the resolver that produced it and the flags it sets.
    /// </summary>
    /// <remarks>
    /// Additive and optional: a consumer that does not know this field ignores it, and a result
    /// from an older installer simply omits it. Present on every successful scripted install,
    /// including the headless preset path. Null for installs with no FOMOD script, and for
    /// script types that do not produce an option tree (e.g. C# scripts).
    /// </remarks>
    public InstallSnapshot OptionSnapshot { get; set; }
}