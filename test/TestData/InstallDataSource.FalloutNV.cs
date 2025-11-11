using FomodInstaller.Interface;

using SharpCompress.Archives;

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TestData;

using static FalloutNVTestData;

file class FalloutNVTestData
{
    public record FalloutNVMod
    {
        public required string Name { get; init; }
        public required string ModFile { get; init; }
        public required Dictionary<string, List<InstallInstruction>> InstructionsByChoice { get; init; }
    }

    public static List<string> StopPatterns =
    [
        "(^|/)fomod(/|$)",
        "[^/]*\\.esp$",
        "[^/]*\\.esm$",
        "[^/]*\\.esl$",
        "[^/]*\\.bsa$",
        "[^/]*\\.ba2$",
        "fomod/ModuleConfig.xml$",
        "(^|/)distantlod(/|$)",
        "(^|/)textures(/|$)",
        "(^|/)meshes(/|$)",
        "(^|/)music(/|$)",
        "(^|/)shaders(/|$)",
        "(^|/)video(/|$)",
        "(^|/)interface(/|$)",
        "(^|/)fonts(/|$)",
        "(^|/)scripts(/|$)",
        "(^|/)facegen(/|$)",
        "(^|/)menus(/|$)",
        "(^|/)lodsettings(/|$)",
        "(^|/)lsdata(/|$)",
        "(^|/)sound(/|$)",
        "(^|/)strings(/|$)",
        "(^|/)trees(/|$)",
        "(^|/)asi(/|$)",
        "(^|/)tools(/|$)",
        "(^|/)calientetools(/|$)",
        "(^|/)nvse(/|$)"
    ];

    public static FalloutNVMod ModTheModConfigurationMenu = new()
    {
        Name = "The Mod Configuration Menu",
        ModFile = "NVAC - New Vegas Anti Crash-53635-7-5-1-0.zip",
        InstructionsByChoice = new Dictionary<string, List<InstallInstruction>>
        {
            // C# script installer - installs all files except fomod folder, includes_StartMenu.xml, and start_menu.xml (initially)
            // Then modifies/creates the XML files with MCM includes
            ["Default"] =
            [
                new() { type = @"copy", source = @"The Mod Configuration Menu\Config\MCM.ini", destination = @"Config\MCM.ini" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\list_box_template.xml", destination = @"menus\prefabs\list_box_template.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\MCM\List.xml", destination = @"menus\prefabs\MCM\List.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\MCM\ListRect.xml", destination = @"menus\prefabs\MCM\ListRect.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\MCM\MCM.xml", destination = @"menus\prefabs\MCM\MCM.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\MCM\ModList.xml", destination = @"menus\prefabs\MCM\ModList.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\MCM\ModListRect.xml", destination = @"menus\prefabs\MCM\ModListRect.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\MCM\ModTitleRect.xml", destination = @"menus\prefabs\MCM\ModTitleRect.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\MCM\OptionRect.xml", destination = @"menus\prefabs\MCM\OptionRect.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\MCM\Options.xml", destination = @"menus\prefabs\MCM\Options.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\menus\prefabs\MCM\Scale.xml", destination = @"menus\prefabs\MCM\Scale.xml" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\NVSE\Plugins\MCM.dll", destination = @"NVSE\Plugins\MCM.dll" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\Readme - The Mod Configuration Menu.txt", destination = @"Readme - The Mod Configuration Menu.txt" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\textures\MCM\Arrow.dds", destination = @"textures\MCM\Arrow.dds" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\textures\MCM\Check0.dds", destination = @"textures\MCM\Check0.dds" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\textures\MCM\Check1.dds", destination = @"textures\MCM\Check1.dds" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\textures\MCM\Icon.dds", destination = @"textures\MCM\Icon.dds" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\textures\MCM\Indicator1.dds", destination = @"textures\MCM\Indicator1.dds" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\textures\MCM\MCM.dds", destination = @"textures\MCM\MCM.dds" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\textures\MCM\Meter.dds", destination = @"textures\MCM\Meter.dds" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\textures\MCM\MeterArrow.dds", destination = @"textures\MCM\MeterArrow.dds" },
                new() { type = @"copy", source = @"The Mod Configuration Menu\The Mod Configuration Menu.esp", destination = @"The Mod Configuration Menu.esp" },
                // Script also generates/modifies these files:
                new() { type = @"generatefile", source = @"The Mod Configuration Menu\menus\options\start_menu.xml", destination = @"menus\options\start_menu.xml" },
                new() { type = @"generatefile", source = @"The Mod Configuration Menu\menus\prefabs\includes_StartMenu.xml", destination = @"menus\prefabs\includes_StartMenu.xml" },
                new() { type = @"enableallplugins" },
            ],
        },
    };

    public static IArchive GetFalloutNVMod(string mod)
    {
        var resource = typeof(InstallDataSource).Assembly.GetManifestResourceStream($"TestData.Data.FalloutNV.{mod}")!;
        return ArchiveFactory.Open(resource);
    }

    public static InstallData BaseTest(FalloutNVMod mod, string choice, IEnumerable<SelectedOption>? dialogChoices, JsonDocument? preset, List<string> installedPlugins) => new()
    {
        Game = "FalloutNV",
        Mod = mod.Name,
        ModArchive = GetFalloutNVMod(mod.ModFile),
        StopPatterns = StopPatterns,
        PluginPath = "Data",
        AppVersion = "1.0.0",
        GameVersion = "1.4.0.525",
        ExtenderVersion = "0.6.21",
        DialogChoices = dialogChoices,
        Preset = preset,
        InstalledPlugins = installedPlugins,
        Validate = true,
        Message = "Installation successful",
        Instructions = mod.InstructionsByChoice[choice],
    };
}

public partial class InstallDataSource
{
    public static IEnumerable<Func<InstallData>> FalloutNVData()
    {
        // C# script-based installer - no dialog choices needed (script handles UI internally)
        yield return () => BaseTest(ModTheModConfigurationMenu, "Default", null, null, []);
    }
}