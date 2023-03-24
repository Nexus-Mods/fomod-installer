﻿namespace FomodInstaller.Interface
{
    public struct Instruction
    {
        public static Instruction CreateCopy(string source, string destination, int priority)
        {
            return new Instruction()
            {
                type = "copy",
                source = source,
                destination = forceRelative(destination),
                priority = priority,
            };
        }

        public static Instruction CreateMKDir(string destination)
        {
            return new Instruction()
            {
                type = "mkdir",
                destination = forceRelative(destination),
            };
        }

        public static Instruction GenerateFile(byte[] byteSource, string destination)
        {
            return new Instruction()
            {
                type = "generatefile",
                data = byteSource,
                destination = forceRelative(destination),
            };
        }

        public static Instruction CreateIniEdit(string fileName, string section, string key, string value)
        {
            return new Instruction()
            {
                type = "iniedit",
                destination = fileName,
                section = section,
                key = key,
                value = value,
            };
        }

        public static Instruction EnablePlugin(string plugin)
        {
            return new Instruction()
            {
                type = "enableplugin",
                source = plugin,
            };
        }

        public static Instruction EnableAllPlugins()
        {
            return new Instruction()
            {
                type = "enableallplugins",
            };
        }

        public static Instruction UnsupportedFunctionalityWarning(string function)
        {
            return new Instruction()
            {
                type = "unsupported",
                source = function,
            };
        }

        public static Instruction InstallError(string severity, string message)
        {
            return new Instruction()
            {
                type = "error",
                source = message,
                value = severity,
            };
        }

        public string type;
        public string source;
        public string destination;
        public string section;
        public string key;
        public string value;
        public byte[] data;
        public int priority;

        private static int sepspn(string input)
        {
          for (var i = 0; i < input.Length; ++i)
          {
            char ch = input[i];
            if (ch != Path.DirectorySeparatorChar && ch != Path.AltDirectorySeparatorChar)
            {
              return i;
            }
          }
          return input.Length;
        }

        private static string forceRelative(string input)
        {
            return input.Remove(0, sepspn(input));
        }
    }
}
