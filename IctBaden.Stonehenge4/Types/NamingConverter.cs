﻿using System.Globalization;
using System.Linq;
using System.Text;

// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Types;

public static class NamingConverter
{
    public static string PascalToKebabCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;

        var builder = new StringBuilder();
        builder.Append(char.ToLower(str.First(), CultureInfo.InvariantCulture));

        foreach (var c in str.Skip(1))
        {
            if (char.IsUpper(c))
            {
                builder.Append('-');
                builder.Append(char.ToLower(c, CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    public static string KebabToPascalCase(string str)
    {
        return ToPascalCase(str, '-');
    }

    public static string SnakeToPascalCase(string str)
    {
        return ToPascalCase(str, '_');
    }

    private static string ToPascalCase(string str, char delimiter)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;

        var builder = new StringBuilder();
        builder.Append(char.ToUpper(str.First(), CultureInfo.InvariantCulture));

        var dash = false;
        foreach (var c in str.Skip(1))
        {
            if (c == delimiter)
            {
                dash = true;
            }
            else if (dash)
            {
                builder.Append(char.ToUpper(c, CultureInfo.InvariantCulture));
                dash = false;
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}