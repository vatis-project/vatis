using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Atis.Extensions;

public static class NumberFormatExtensions
{
    /// <summary>
    /// Converts the number to group form for added clarity
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <returns>Returns the number in group word form. For example: 10,500 would yield "ten thousand five hundred"</returns>
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

        var words = "";

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
            if (words != "")
                words += "and ";

            var unitsMap = new[]
            {
                "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven",
                "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
            };
            var tensMap = new[]
                { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if (number % 10 > 0)
                    words += "-" + unitsMap[number % 10];
            }
        }

        return words;
    }

    /// <summary>
    /// Converts the numebr into a word string.
    /// </summary>
    /// <param name="number">The number to convert</param>
    /// <returns>Returns the numebr in word string format. For example, 10,500 would yield "one zero thousand five hundred"</returns>
    public static string ToWordString(this IConvertible value)
    {
        var number = Convert.ToInt32(value);

        var isNegative = number < 0;

        number = Math.Abs(number);

        if (number == 0)
            return "zero";

        if (isNegative)
            return "minus " + Math.Abs(number).ToWordString();

        string words = "";

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
                "one two", "one three", "one four", "one five", "one six", "one seven", "one eight", "one niner"
            };
            var tensMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "niner" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if (number % 10 >= 0)
                    words += " " + unitsMap[number % 10];
            }
        }

        return words.TrimEnd(' ');
    }

    /// <summary>
    /// Formats a number into serial format
    /// </summary>
    /// <param name="number">The number to format.</param>
    /// <param name="useDecimalTerminology">Whether to use "decimal" vs "point" terminology.</param>
    /// <returns>Returns the number formatted in serial format. For example, 1500 would yield "one five zero zero".</returns>
    public static string? ToSerialFormat(this string? number, bool useDecimalTerminology = false)
    {
        var group = new List<string>();
        if (number != null && Regex.IsMatch(number, @"[0-9]\d*(\.\d+)?"))
        {
            foreach (var numberPart in number.Split('.'))
            {
                var temp = new List<string>();
                foreach (var digit in numberPart.ToString().Select(q => new string(q, 1)).ToArray())
                {
                    temp.Add(int.Parse(digit).ToWordString());
                }

                group.Add(string.Join(" ", temp).Trim(' '));
            }

            return string.Join(useDecimalTerminology ? " decimal " : " point ", group);
        }

        return number;
    }

    /// <summary>
    /// Converts a number into serial format
    /// </summary>
    /// <param name="number">The number to format</param>
    /// <param name="leadingZero">Whether to prefix number with leading zero if less than 10.</param>
    /// <returns>Returns the number in serial format.</returns>
    public static string ToSerialFormat(this IConvertible value, bool leadingZero = false)
    {
        var number = Convert.ToInt32(value);

        List<string> temp = [];

        if (number < 10 && leadingZero)
            temp.Add("zero");

        foreach (var x in Math.Abs(number).ToString().Select(q => new string(q, 1)).ToArray())
        {
            temp.Add(int.Parse(x).ToWordString());
        }

        return $"{(number < 0 ? "minus " : "")}{string.Join(" ", temp)}";
    }

    public static int NormalizeHeading(this IConvertible value)
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

    public static int ApplyMagVar(this IConvertible value, int? magVar = null)
    {
        var degrees = Convert.ToInt32(value);

        if (magVar == null)
            return degrees;

        if (degrees == 0)
            return degrees;

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

    public static int ToFsdFrequencyFormat(this decimal value)
    {
        return (int)((value - 100m) * 1000m);
    }
}