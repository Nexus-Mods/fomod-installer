using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Utils
{
    public interface IFileSystem
    {
	    byte[]? ReadFileContent(string filePath, int offset, int length);
	    string[]? ReadDirectoryFileList(string directoryPath, string pattern, SearchOption searchOption);
	    string[]? ReadDirectoryList(string directoryPath);
    }
    
    public class DefaultFileSystem : IFileSystem
	{
		public byte[] ReadFileContent(string filePath, int offset, int length)
		{
			if (!File.Exists(filePath)) return null;
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			if (offset < 0 || offset > fs.Length) throw new ArgumentOutOfRangeException(nameof(offset));
			if (length < -1) throw new ArgumentOutOfRangeException(nameof(length));
			if (length == -1) length = (int)(fs.Length - offset);
			if (offset + length > fs.Length) throw new ArgumentOutOfRangeException(nameof(length));

			var buffer = new byte[length];
			fs.Seek(offset, SeekOrigin.Begin);
			var read = fs.Read(buffer, 0, length);
			if (read < length)
			{
				Array.Resize(ref buffer, read);
			}
			return buffer;
		}

		public string[] ReadDirectoryFileList(string directoryPath, string pattern, SearchOption searchOption)
		{
			if (!Directory.Exists(directoryPath)) return null;
			return Directory.GetFiles(directoryPath, pattern, searchOption);
		}

		public string[] ReadDirectoryList(string directoryPath)
		{
			if (!Directory.Exists(directoryPath)) return null;
			return Directory.GetDirectories(directoryPath);
		}
	}
    
    public static class FileSystem
	{
		public static IFileSystem Instance { get; set; } = new DefaultFileSystem();
		
		/// <summary>
		/// Test if a file exists
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static bool FileExists(string filePath)
		{
			return Instance.ReadFileContent(filePath, 0, 1) != null;
		}

        /// <summary>
        /// Test if a directory exists
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool DirectoryExists(string directoryPath)
        {
	        var parentDir = Path.GetDirectoryName(directoryPath);
	        if (parentDir == null) return false;
	        return Instance.ReadDirectoryList(parentDir)?.Contains(directoryPath) == true;
        }

        /// <summary>
        /// Read whole content of a file into an array linewise
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string[] ReadAllLines(string filePath)
		{
			var data = Instance.ReadFileContent(filePath, 0, -1) ??
			           throw new FileNotFoundException($"File not found: {filePath}");;
			return Encoding.UTF8.GetString(data ?? []).Split(["\r\n", "\n"], StringSplitOptions.None);
		}

        /// <summary>
        /// Read whole uninterpreted content of a file into a byte array
        /// </summary>
        /// <param name="filePath">path to the file to read</param>
        /// <returns></returns>
        public static byte[] ReadAllBytes(string filePath)
		{
			return Instance.ReadFileContent(filePath, 0, -1) ??
			       throw new FileNotFoundException($"File not found: {filePath}");
		}

        /// <summary>
        /// Opens a file stream
        /// </summary>
        /// <param name="filePath">path of the file to open</param>
        /// <param name="fileMode">mode</param>
        /// <returns></returns>
        public static Stream Open(string filePath, FileMode fileMode)
		{
			var data = Instance.ReadFileContent(filePath, 0, -1) ??
			           (fileMode == FileMode.Open ? throw new FileNotFoundException($"File not found: {filePath}") : null);
			return new MemoryStream(data ?? []);
		}

        /// <summary>
        /// Retrieve file attributes for a file
        /// </summary>
        /// <param name="filePath">path of the file</param>
        /// <returns>attributes or null if the file doesn't exist</returns>
        public static FileAttributes GetAttributes(string filePath)
		{
			return File.GetAttributes(filePath);
		}

        /// <summary>
        /// Verifies if the given path is safe to be written to.
        /// </summary>
        /// <remarks>
        /// A path is safe to be written to if it contains no charaters
        /// disallowed by the operating system, and if is is in the Data
        /// directory or one of its sub-directories.
        /// </remarks>
        /// <param name="p_strPath">The path whose safety is to be verified.</param>
        /// <returns><c>true</c> if the given path is safe to write to;
        /// <c>false</c> otherwise.</returns>
        public static bool IsSafeFilePath(string p_strPath)
        {
            if (p_strPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                return false;
            if (Path.IsPathRooted(p_strPath))
                return false;
            if (p_strPath.Contains(".." + Path.AltDirectorySeparatorChar))
                return false;
            if (p_strPath.Contains(".." + Path.DirectorySeparatorChar))
                return false;
            return true;
        }

        /// <summary>
        /// Retrieves the list of files in the specified path that match a pattern
        /// </summary>
        /// <param name="searchPath">path to search in</param>
        /// <param name="pattern">the patterns have to match</param>
        /// <param name="searchOption">options to control how the search works (i.e. recursive vs. only the specified dir)</param>
        /// <returns></returns>
        public static string[] GetFiles(string searchPath, string pattern, SearchOption searchOption)
        {
	        return Instance.ReadDirectoryFileList(searchPath, pattern, searchOption);
        }
    }
}
