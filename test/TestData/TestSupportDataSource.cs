using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestData;

public class TestSupportDataSource
{
    // Use platform-specific path separator for cross-platform compatibility
    private static string P(string path) => path.Replace('/', Path.DirectorySeparatorChar);

    public static IEnumerable<Func<TestSupportData>> BasicData()
    {
        yield return () => new(["test.esm", "test.esp"], ["Basic"], true, []);

        yield return () => new([], ["CSharpScript"], false, []);
    }

    public static IEnumerable<Func<TestSupportData>> CSharpData()
    {
        yield return () => new(["test.esm", "test.esp"], ["CSharpScript"], false, []);

        yield return () => new([], ["Basic"], false, []);
    }

    public static IEnumerable<Func<TestSupportData>> XmlData()
    {
        yield return () => new([], ["XmlScript"], false, []);
        yield return () => new(["test.esm", "test.esp"], ["XmlScript"], false, []);

        yield return () => new(["test.esm", "test.esp", "script.xml"], ["XmlScript"], false, []);
        // Required is duplicated for some reason
        yield return () => new(["test.esm", "test.esp", P("fomod/script.xml")], ["XmlScript"], true, [P("fomod/script.xml"), P("fomod/script.xml")]);
        yield return () => new(["test.esm", "test.esp", "ModuleConfig.xml"], ["XmlScript"], false, []);
        // Required is duplicated for some reason
        yield return () => new(["test.esm", "test.esp", P("fomod/ModuleConfig.xml")], ["XmlScript"], true, [P("fomod/ModuleConfig.xml"), P("fomod/ModuleConfig.xml")]);
        yield return () => new(["test.esm", "test.esp", "script.xml", "ModuleConfig.xml"], ["XmlScript"], false, []);
        // Required is duplicated for some reason
        yield return () => new(["test.esm", "test.esp", P("fomod/script.xml"), P("fomod/ModuleConfig.xml")], ["XmlScript"], true, [P("fomod/script.xml"), P("fomod/ModuleConfig.xml"), P("fomod/script.xml"), P("fomod/ModuleConfig.xml")]);
    }

    public static IEnumerable<Func<TestSupportData>> DynamicData()
    {
        yield break;
    }

    public static IEnumerable<Func<TestSupportData>> LiteData()
    {
        // Whatever OMOD was - it's not supported
        yield return () => new(["test.esm", "test.esp", "script"], ["XmlScript"], false, []);
        yield return () => new(["test.esm", "test.esp", "script.txt"], ["XmlScript"], false, []);

        yield return () => new(["test.esm", "test.esp"], [], false, []);
        yield return () => new([], [], false, []);

        yield return () => new(["test.esm", "test.esp"], ["Basic"], true, []);
        yield return () => new([], ["CSharpScript"], false, []);

        yield return () => new(["test.esm", "test.esp"], ["CSharpScript"], false, []);
        yield return () => new([], ["CSharpScript"], false, []);
    }
}