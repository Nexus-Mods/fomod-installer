using System;

namespace ModInstaller.Native;

internal static class BoolExtensions
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

    public static FormattableString ToFormattable(this bool value) => new(value ? "true" : "false");
}