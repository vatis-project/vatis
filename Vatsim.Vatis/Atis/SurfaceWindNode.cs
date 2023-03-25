using System.Collections.Generic;
using Vatsim.Vatis.Utils;
using Vatsim.Vatis.Weather.Extensions;
using Vatsim.Vatis.Weather.Objects;

namespace Vatsim.Vatis.Atis;

public class SurfaceWindNode : AtisNode
{
    public SurfaceWindNode()
    { }

    public override void Parse(Metar metar)
    {
        Parse(metar.SurfaceWind);
    }

    public void Parse(SurfaceWind node)
    {
        List<string> tts = new();
        List<string> acars = new();

        var magVarDeg = Composite.MagneticVariation?.MagneticDegrees ?? null;

        if (node == null)
            return;

        var windUnitSpoken = "";
        switch (node.WindUnit)
        {
            case Weather.Enums.WindUnit.KilometersPerHour:
                windUnitSpoken = node.Speed > 1 ? "kilometers per hour" : "kilometer per hour";
                break;
            case Weather.Enums.WindUnit.MetersPerSecond:
                windUnitSpoken = node.Speed > 1 ? "meters per second" : "meter per second";
                break;
            case Weather.Enums.WindUnit.Knots:
                windUnitSpoken = node.Speed > 1 ? "knots" : "knot";
                break;
        }

        var windUnitText = EnumTranslator.GetEnumDescription(node.WindUnit);

        if (node.GustSpeed > 0)
        {
            // VRB10G20KT
            if (node.IsVariable)
            {
                if (Composite.UseSurfaceWindPrefix)
                {
                    tts.Add($"Surface wind variable {node.Speed.NumberToSingular()} gusts {node.GustSpeed.NumberToSingular()}");
                }
                else
                {
                    tts.Add($"Wind variable at {node.Speed.NumberToSingular()} gusts {node.GustSpeed.NumberToSingular()}");
                }

                acars.Add($"VRB{node.Speed:00}G{node.GustSpeed:00}{windUnitText}");
            }
            // 25010G16KT
            else
            {
                if (!Composite.UseFaaFormat)
                {
                    tts.Add($"{(Composite.UseSurfaceWindPrefix ? "Surface Wind " : "Wind ")}{node.Direction.ApplyMagVar(magVarDeg).ToString("000").NumberToSingular()} degrees, {node.Speed.NumberToSingular()} {windUnitSpoken} gusts {node.GustSpeed.NumberToSingular()}");
                }
                else
                {
                    tts.Add($"Wind {node.Direction.ApplyMagVar(magVarDeg).ToString("000").NumberToSingular()} at {node.Speed.NumberToSingular()} gusts {node.GustSpeed.NumberToSingular()}");
                }

                acars.Add($"{node.Direction.ApplyMagVar(magVarDeg):000}{node.Speed:00}G{node.GustSpeed:00}{windUnitText}");
            }
        }
        // 25010KT
        else
        {
            if (node.Direction > 0)
            {
                if (!Composite.UseFaaFormat)
                {
                    tts.Add($"{(Composite.UseSurfaceWindPrefix ? "Surface Wind " : "Wind ")}{node.Direction.ApplyMagVar(magVarDeg).ToString("000").NumberToSingular()} degrees, {node.Speed.NumberToSingular()} {windUnitSpoken}");
                }
                else
                {
                    tts.Add($"Wind {node.Direction.ApplyMagVar(magVarDeg).ToString("000").NumberToSingular()} at {node.Speed.NumberToSingular()}");
                }
            }
            else if (node.Direction == 0 && node.Speed == 0)
            {
                tts.Add("Wind calm");
            }

            acars.Add($"{node.Direction.ApplyMagVar(magVarDeg):000}{node.Speed:00}{windUnitText}");
        }

        // VRB10KT
        if (node.GustSpeed == 0 && node.IsVariable)
        {
            if (!Composite.UseFaaFormat)
            {
                tts.Add($"{(Composite.UseSurfaceWindPrefix ? "Surface Wind " : "Wind ")}variable at {node.Speed.NumberToSingular()} {windUnitSpoken}");
            }
            else
            {
                tts.Add($"Wind variable at {node.Speed.NumberToSingular()}");
            }

            acars.Add($"VRB{node.Speed:00}{windUnitText}");
        }

        // 250V360
        if (node.ExtremeWindDirections != null)
        {
            if (!Composite.UseFaaFormat)
            {
                tts.Add($"Varying between {node.ExtremeWindDirections.FirstExtremeDirection.ApplyMagVar(magVarDeg).ToString("000").NumberToSingular()} and {node.ExtremeWindDirections.LastExtremeWindDirection.ApplyMagVar(magVarDeg).ToString("000").NumberToSingular()} degrees");
            }
            else
            {
                tts.Add($"Wind variable between {node.ExtremeWindDirections.FirstExtremeDirection.ApplyMagVar(magVarDeg).ToString("000").NumberToSingular()} and {node.ExtremeWindDirections.LastExtremeWindDirection.ApplyMagVar(magVarDeg).ToString("000").NumberToSingular()}");
            }

            acars.Add($"{node.ExtremeWindDirections.FirstExtremeDirection.ApplyMagVar(magVarDeg):000}V{node.ExtremeWindDirections.LastExtremeWindDirection.ApplyMagVar(magVarDeg):000}");
        }

        VoiceAtis = string.Join(", ", tts).TrimEnd(',').TrimEnd(' ');
        TextAtis = string.Join(" ", acars).TrimEnd(' ');
    }
}