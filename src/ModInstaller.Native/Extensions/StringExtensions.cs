using System;

namespace ModInstaller.Native;

internal static class StringExtensions
{
    internal readonly struct FormattableString : IFormattable
    {
        private readonly string _string;

        public FormattableString(string str)
        {
            _string = str;
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => _string;
    }

    public static FormattableString ToFormattable(this string str) => new(str);
}