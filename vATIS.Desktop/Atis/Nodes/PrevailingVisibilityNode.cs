using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;
public class PrevailingVisibilityNode : BaseNode<Visibility>
{
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.Visibility);
    }

    private void Parse(Visibility? node)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (node == null)
            return;

        VoiceAtis = ParseVoiceVariables(node, Station.AtisFormat.Visibility.Template.Voice);
        TextAtis = ParseTextVariables(node, Station.AtisFormat.Visibility.Template.Text);
    }

    public override string ParseTextVariables(Visibility value, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        if (value.IsCavok)
        {
            return "CAVOK";
        }
        else
        {
            if (value.PrevailingVisibility != null &&
                value.PrevailingVisibility.ActualUnit == Value.Unit.Meter &&
                (int)value.PrevailingVisibility.ActualValue == 9999)
            {
                return Station.AtisFormat.Visibility.UnlimitedVisibilityText;
            }

            if (value.RawValue != null)
                return Regex.Replace(format, "{visibility}", value.RawValue, RegexOptions.IgnoreCase);
        }
        return "";
    }

    public override string ParseVoiceVariables(Visibility node, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        var parsedValue = new List<string>();

        if (node.IsCavok)
        {
            return "KAV-OH-KAY";
        }
        else
        {
            if (node.PrevailingVisibility != null && node.PrevailingVisibility.ActualUnit == Value.Unit.Meter)
            {
                if ((int)node.PrevailingVisibility.ActualValue == 9999)
                {
                    return Station.AtisFormat.Visibility.UnlimitedVisibilityVoice;
                }

                if (!string.IsNullOrEmpty(node.MinimumVisibilityDirection))
                {
                    if (node.MinimumVisibility != null)
                    {
                        var minVisibility = (int)node.MinimumVisibility.ActualValue;
                        switch (node.MinimumVisibilityDirection)
                        {
                            case "N":
                                parsedValue.Add($"{Station.AtisFormat.Visibility.North} {minVisibility.ToGroupForm()}");
                                break;
                            case "NE":
                                parsedValue.Add($"{Station.AtisFormat.Visibility.NorthEast} {minVisibility.ToGroupForm()}");
                                break;
                            case "E":
                                parsedValue.Add($"{Station.AtisFormat.Visibility.East} {minVisibility.ToGroupForm()}");
                                break;
                            case "SE":
                                parsedValue.Add($"{Station.AtisFormat.Visibility.SouthEast} {minVisibility.ToGroupForm()}");
                                break;
                            case "S":
                                parsedValue.Add($"{Station.AtisFormat.Visibility.South} {minVisibility.ToGroupForm()}");
                                break;
                            case "SW":
                                parsedValue.Add($"{Station.AtisFormat.Visibility.SouthWest} {minVisibility.ToGroupForm()}");
                                break;
                            case "W":
                                parsedValue.Add($"{Station.AtisFormat.Visibility.West} {minVisibility.ToGroupForm()}");
                                break;
                            case "NW":
                                parsedValue.Add($"{Station.AtisFormat.Visibility.NorthWest} {minVisibility.ToGroupForm()}");
                                break;
                        }
                    }

                    if (Station.AtisFormat.Visibility.IncludeVisibilitySuffix)
                    {
                        parsedValue.Add(
                            node.PrevailingVisibility.ActualValue > Station.AtisFormat.Visibility.MetersCutoff
                                ? "kilometers"
                                : "meters");
                    }
                }
                else
                {
                    if (node.PrevailingVisibility.ActualValue > Station.AtisFormat.Visibility.MetersCutoff)
                    {
                        parsedValue.Add($"{node.PrevailingVisibility.ActualValue / 1000} {(Station.AtisFormat.Visibility.IncludeVisibilitySuffix ? "kilometers" : "")}");
                    }
                    else
                    {
                        var vis = (int)node.PrevailingVisibility.ActualValue;
                        parsedValue.Add($"{vis.ToWordString()} {(Station.AtisFormat.Visibility.IncludeVisibilitySuffix ? "meters" : "")}");
                    }
                }
            }
            else
            {
                if (node.RawValue != null && node.RawValue.Contains('/'))
                {
                    string result = node.RawValue switch
                    {
                        "M1/4SM" => "less than one quarter.",
                        "1 1/8SM" => "one and one eighth.",
                        "1 1/4SM" => "one and one quarter.",
                        "1 3/8SM" => "one and three eighths.",
                        "1 1/2SM" => "one and one half.",
                        "1 5/8SM" => "one and five eighths.",
                        "1 3/4SM" => "one and three quarters.",
                        "1 7/8SM" => "one and seven eighths.",
                        "2 1/4SM" => "two and one quarter.",
                        "2 1/2SM" => "two and one half.",
                        "2 3/4SM" => "two and three quarters.",
                        "1/16SM" => "one sixteenth.",
                        "1/8SM" => "one eighth.",
                        "3/16SM" => "three sixteenths.",
                        "1/4SM" => "one quarter.",
                        "5/16SM" => "five sixteenths.",
                        "3/8SM" => "three eighths.",
                        "1/2SM" => "one half.",
                        "5/8SM" => "five eighths.",
                        "3/4SM" => "three quarters.",
                        "7/8SM" => "seven eighths.",
                        _ => ""
                    };

                    parsedValue.Add(result);
                }
                else
                {
                    if (node.PrevailingVisibility != null)
                    {
                        var vis = (int)node.PrevailingVisibility.ActualValue;
                        parsedValue.Add(vis.ToString());
                    }
                }
            }

            return Regex.Replace(format, "{visibility}", string.Join(", ", parsedValue), RegexOptions.IgnoreCase);
        }
    }
}
