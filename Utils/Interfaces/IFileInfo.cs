namespace FomodInstaller.Utils.Interfaces
{
	public interface IFileInfo
	{
		FileAttributes Attributes { get; }
		bool IsReadOnly { get; }
	}
}
