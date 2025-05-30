// <copyright file="NumberFormatExtensions.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Provides extension methods for numeric values to convert them into various textual and formatted representations.
/// </summary>
public static class NumberFormatExtensions
{
    /// <summary>
    /// Converts a numeric value to a formatted string representation, either in group format or serial format.
    /// </summary>
    /// <param name="value">The value to format. Must implement <see cref="IConvertible"/>.</param>
    /// <param name="speakInGroupFormat">
    /// If <c>true</c>, formats the value using the group format (e.g., digit-by-digit with spacing).
    /// If <c>false</c>, uses the serial format.
    /// </param>
    /// <param name="speakLeadingZero">
    /// Optional. If <c>true</c>, includes a leading zero when using the serial format.
    /// This parameter is ignored when <paramref name="speakInGroupFormat"/> is <c>true</c>.
    /// </param>
    /// <returns>A formatted string representation of the value.</returns>
    public static string ToFormat(this IConvertible value, bool speakInGroupFormat,
        bool speakLeadingZero = false)
    {
        return speakInGroupFormat ? ToGroupForm(value) : ToSerialFormat(value, speakLeadingZero);
    }

    /// <summary>
    /// Converts the value into a group form.
    /// </summary>
    /// <param name="value">The number to convert.</param>
    /// <returns>The value in group form. For example, 10,500 would yield "ten thousand five hundred".</returns>
    public static string ToGroupForm(this IConvertible value)
    {
        var number = Convert.ToInt32(value);

        switch (number)
        {
            case 0:
                return "zero";
            case < 0:
                return "minus " + Math.Abs(number).ToGroupForm();
        }

        var words = string.Empty;

        if (number / 1000000 > 0)
        {
            words += (number / 1000000).ToGroupForm() + " million ";
            number %= 1000000;
        }

        if (number / 1000 > 0)
        {
            words += (number / 1000).ToGroupForm() + " thousand ";
            number %= 1000;
        }

        if (number / 100 > 0)
        {
            words += (number / 100).ToGroupForm() + " hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            if (words != string.Empty)
            {
                words += "and ";
            }

            var unitsMap = new[]
            {
                "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven",
                "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen",
            };
            var tensMap = new[]
            {
                "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety",
            };

            if (number < 20)
            {
                words += unitsMap[number];
            }
            else
            {
                words += tensMap[number / 10];
                if (number % 10 > 0)
                {
                    words += "-" + unitsMap[number % 10];
                }
            }
        }

        return words;
    }

    /// <summary>
    /// Converts the value into a word string.
    /// </summary>
    /// <param name="value">The number to convert.</param>
    /// <returns>The value in word string format. For example, 10,500 would yield "one zero thousand five hundred".</returns>
    public static string ToWordString(this IConvertible value)
    {
        var number = Convert.ToInt32(value);

        var isNegative = number < 0;

        number = Math.Abs(number);

        if (number == 0)
        {
            return "zero";
        }

        if (isNegative)
        {
            return "minus " + number.ToWordString();
        }

        var words = string.Empty;

        if (number / 1000000 > 0)
        {
            words += (number / 1000000).ToWordString() + " million ";
            number %= 1000000;
        }

        if (number / 1000 > 0)
        {
            words += (number / 1000).ToWordString() + " thousand ";
            number %= 1000;
        }

        if (number / 100 > 0)
        {
            words += (number / 100).ToWordString() + " hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            var unitsMap = new[]
            {
                "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "niner", "one zero", "one one",
                "one two", "one three", "one four", "one five", "one six", "one seven", "one eight", "one niner",
            };
            var tensMap = new[]
            {
                "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "niner",
            };

            if (number < 20)
            {
                words += unitsMap[number];
            }
            else
            {
                words += tensMap[number / 10];
                if (number % 10 >= 0)
                {
                    words += " " + unitsMap[number % 10];
                }
            }
        }

        return words.TrimEnd(' ');
    }

    /// <summary>
    /// Formats a number into serial format.
    /// </summary>
    /// <param name="number">The number to format.</param>
    /// <param name="useDecimalTerminology">Whether to use "decimal" vs "point" terminology.</param>
    /// <returns>Returns the number formatted in serial format. For example, 1530 would yield "one five three zero".</returns>
    public static string? ToSerialFormat(this string? number, bool useDecimalTerminology = false)
    {
        if (string.IsNullOrEmpty(number) || !Regex.IsMatch(number, @"^-?[0-9]+(\.[0-9]+)?$"))
        {
            return number;
        }

        var group = new List<string>();
        var isNegative = number.StartsWith('-');
        var normalizedNumber = isNegative ? number[1..] : number;

        foreach (var numberPart in normalizedNumber.Split('.'))
        {
            var temp = new List<string>();
            foreach (var digit in numberPart.Select(ch => new string(ch, 1)))
            {
                temp.Add(int.Parse(digit).ToWordString());
            }

            group.Add(string.Join(" ", temp).Trim());
        }

        var result = string.Join(useDecimalTerminology ? " decimal " : " point ", group);
        return isNegative ? "minus " + result : result;
    }

    /// <summary>
    /// Converts a number into serial format.
    /// </summary>
    /// <param name="value">The number to format.</param>
    /// <param name="leadingZero">Whether to prefix number with leading zero if less than 10.</param>
    /// <returns>Returns the number in serial format.</returns>
    public static string ToSerialFormat(this IConvertible value, bool leadingZero = false)
    {
        var number = Convert.ToInt32(value);

        List<string> temp = [];

        if (number < 10 && leadingZero)
        {
            temp.Add("zero");
        }

        foreach (var x in Math.Abs(number).ToString().Select(q => new string(q, 1)).ToArray())
        {
            temp.Add(int.Parse(x).ToWordString());
        }

        return $"{(number < 0 ? "minus " : string.Empty)}{string.Join(" ", temp)}";
    }

    /// <summary>
    /// Applies magnetic variation to a heading.
    /// </summary>
    /// <param name="value">The heading to apply magnetic variation to.</param>
    /// <param name="enabled">Whether magnetic variation is enabled.</param>
    /// <param name="magVar">The magnetic variation to apply.</param>
    /// <returns>The heading with magnetic variation applied.</returns>
    public static int ApplyMagVar(this IConvertible value, bool enabled, int? magVar = null)
    {
        var degrees = Convert.ToInt32(value);

        if (!enabled)
        {
            return degrees;
        }

        if (magVar == null)
        {
            return degrees;
        }

        if (degrees == 0)
        {
            return degrees;
        }

        if (magVar > 0)
        {
            degrees += magVar.Value;
        }
        else
        {
            degrees -= Math.Abs(magVar.Value);
        }

        return degrees.NormalizeHeading();
    }

    /// <summary>
    /// Converts a decimal frequency to a formatted string.
    /// </summary>
    /// <param name="value">The frequency to format.</param>
    /// <returns>The formatted frequency string.</returns>
    public static int ToFsdFrequencyFormat(this decimal value)
    {
        return (int)((value - 100m) * 1000m);
    }

    private static int NormalizeHeading(this IConvertible value)
    {
        var heading = Convert.ToInt32(value);

        switch (heading)
        {
            case <= 0:
                heading += 360;
                break;
            case > 360:
                heading -= 360;
                break;
        }

        return heading;
    }
}
