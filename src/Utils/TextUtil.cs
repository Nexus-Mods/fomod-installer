using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utils
{
    /// <summary>
    /// Utility functions to work with text.
    /// </summary>
    public static class TextUtil
    {
        /// <summary>
        /// Converts the given byte array to a string.
        /// </summary>
        /// <remarks>
        /// This method attempts to detect the text encoding.
        /// </remarks>
        /// <param name="p_bteText">The bytes to convert to a string.</param>
        /// <returns>A string respresented by the given bytes.</returns>
        public static string ByteToString(byte[] p_bteText)
        {
            string strText;
            using (MemoryStream msmFile = new MemoryStream(p_bteText))
            {
                using (StreamReader strReader = new StreamReader(msmFile, true))
                {
                    strText = strReader.ReadToEnd();
                    strReader.Close();
                }
                msmFile.Close();
            }
            return strText;
        }

        /// <summary>
        /// Converts the given byte array to an array of string lines.
        /// </summary>
        /// <remarks>
        /// This method attempts to detect the text encoding.
        /// </remarks>
        /// <param name="p_bteText">The bytes to convert to an array of string lines.</param>
        /// <returns>An array of string lines respresented by the given bytes.</returns>
        public static string[] ByteToStringLines(byte[] p_bteText)
        {
            List<string> lstLines = new List<string>();
            using (MemoryStream msmFile = new MemoryStream(p_bteText))
            {
                using (StreamReader strReader = new StreamReader(msmFile, true))
                {
                    string strLine = null;
                    while ((strLine = strReader.ReadLine()) != null)
                        lstLines.Add(strLine);
                    strReader.Close();
                }
                msmFile.Close();
            }
            return lstLines.ToArray();
        }

        public static string ByteToStringWithoutBOM(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
                return Encoding.UTF8.GetString(data, 3, data.Length - 3);
            if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
            if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
                return Encoding.Unicode.GetString(data, 2, data.Length - 2);

            return Encoding.UTF8.GetString(data);
        }

        public static string NormalizePath(string path, bool dirTerminate = false, bool alternateSeparators = false, bool toLower = true)
        {
            string temp = string.Empty;
            char targetSep = alternateSeparators ? Path.AltDirectorySeparatorChar : Path.DirectorySeparatorChar;

            // Always normalize both forward and back slashes to the target separator
            // This is needed because on Linux, AltDirectorySeparatorChar is also '/',
            // so backslashes from FOMOD XML wouldn't be converted otherwise
            temp = path
                .Replace("\\\\", "\\")  // Collapse double backslashes first
                .Replace("//", "/")     // Collapse double forward slashes
                .Replace('\\', targetSep)
                .Replace('/', targetSep)
                .Trim(targetSep);

            if (toLower)
                temp = temp.ToLowerInvariant();

            if (dirTerminate && (temp.Length > 0))
            {
                temp += targetSep;
            }

            return temp;
        }
    }
}
