using TestData;

namespace ModInstaller.Native.Tests.Extensions;

internal static class TestSourceExtensions
{
    public static IEnumerable<TestSupportData> NUnit(this IEnumerable<Func<TestSupportData>> e) => e.Select(f => f());
    public static IEnumerable<InstallData> NUnit(this IEnumerable<Func<InstallData>> e) => e.Select(f => f());
}