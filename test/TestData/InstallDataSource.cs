using System;
using System.Collections.Generic;

namespace TestData;

/// <summary>
/// JSON-based test data source that loads test cases from shared JSON files.
/// This replaces the hard-coded test data in individual game source files.
/// </summary>
public class InstallDataSource
{
    /// <summary>
    /// Loads Skyrim test cases from skyrim.json
    /// </summary>
    public static IEnumerable<Func<InstallData>> SkyrimData() =>
        JsonTestDataLoader.LoadTestCases("skyrim.json");

    /// <summary>
    /// Loads Fallout 4 test cases from fallout4.json
    /// </summary>
    public static IEnumerable<Func<InstallData>> Fallout4Data() =>
        JsonTestDataLoader.LoadTestCases("fallout4.json");

    /// <summary>
    /// Loads Fallout NV test cases from falloutnv.json
    /// </summary>
    public static IEnumerable<Func<InstallData>> FalloutNVData() =>
        JsonTestDataLoader.LoadTestCases("falloutnv.json");

    /// <summary>
    /// Loads FOMOD compliance test cases from fomod-compliance.json
    /// </summary>
    public static IEnumerable<Func<InstallData>> FomodComplianceTestsData() =>
        JsonTestDataLoader.LoadTestCases("fomod-compliance.json");

    /// <summary>
    /// Loads C# script test cases from csharp-script.json
    /// </summary>
    public static IEnumerable<Func<InstallData>> CSharpTestCaseData() =>
        JsonTestDataLoader.LoadTestCases("csharp-script.json");
}
