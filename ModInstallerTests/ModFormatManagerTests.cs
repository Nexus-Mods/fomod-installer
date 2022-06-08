using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FomodInstaller.ModInstaller.Tests
{
	public class ModFormatManagerTests
	{

		private readonly ITestOutputHelper output;

		public static IEnumerable<object[]> GetRequirementsData()
		{
			yield return new object[] { 0, new string[] { "test.esm", "test.esp" } };
			yield return new object[] { 0, new string[] { } };
		}

		public ModFormatManagerTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Theory]
		[MemberData(nameof(GetRequirementsData))]
		public async Task GetRequirements(int dummy, string[] modFiles)
		{
			 
		Assert.Empty(modFiles);

		ModFormatManager installer = new ModFormatManager();
			var actual = await installer.GetRequirements(modFiles, false, null);

			// Xunit test
			// Assert.Null(actual);
			Assert.NotNull(actual);
			output.WriteLine("This is output from {0}", actual);
		}
	}
}