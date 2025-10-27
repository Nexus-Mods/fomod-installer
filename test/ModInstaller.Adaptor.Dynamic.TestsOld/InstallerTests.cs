using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using FomodInstaller.Interface;
using Xunit.Abstractions;

namespace FomodInstaller.ModInstaller.Tests
{

	public class InstallerTests
	{

		private readonly ITestOutputHelper output;

		public static void progress(int perc) { }

		public static IEnumerable<object[]> TestData()
		{
			yield return new object[] { 0, new List<string> { "test.esm", "test.esp" } };
			yield return new object[] { 0, new List<string> { } };
		}

		public static IEnumerable<object[]> InstallData()
		{
			yield return new object[] { 0, new List<string> { "test.esm", "test.esp" }, new List<string> { }, "C:\\Xunit\\TEST", null, null };
			yield return new object[] { 0, new List<string> { }, new List<string> { }, "C:\\Xunit\\TEST", null, null };
			yield return new object[] { 0, new List<string> { "test.esm", "test.esp" }, new List<string> { }, "", null, null };
		}

		public InstallerTests(ITestOutputHelper output)
		{
			this.output = output;
		}


		[Theory(Skip = "In a broken state")]
		[MemberData(nameof(TestData))]
		public async Task TestSupported(int dummy, List<string> modArchiveFileList)
		{
			Installer installer = new Installer();
			var actual = await installer.TestSupported(modArchiveFileList, new List<string>{ "BasicType" });

			// Xunit test
			Assert.Null(actual);
			Assert.NotNull(actual);
			output.WriteLine("This is output from {0}", actual);
		}

		[Theory(Skip = "In a broken state")]
		// empty ProgressDelegate and CoreDelegates
		[MemberData(nameof(InstallData))]
		public async Task Install(int dummy, List<string> modArchiveFileList, List<string> gameSpecificStopFolders,
            string destinationPath, ProgressDelegate progressDelegate, DynamicCoreDelegates dynamicCoreDelegate)
		{
			/*
			Assert.Empty(modArchiveFileList);
            Assert.Empty(gameSpecificStopFolders);
			Assert.Empty(destinationPath);
			Assert.NotNull(progressDelegate);
			Assert.NotNull(dynamicCoreDelegate);
			*/

			Installer installer = new Installer();
			var actual = await installer.Install(modArchiveFileList, gameSpecificStopFolders, "", destinationPath, null, true, progressDelegate, dynamicCoreDelegate);

			// Xunit test
			Assert.Null(actual);
			Assert.NotNull(actual);
			output.WriteLine("This is output from {0}", actual);
		}
	}
}