﻿using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class SurfaceWindChunkDecoder : MetarChunkDecoder
{
    private const string SurfaceWindParameterName = "SurfaceWind";
    private const string DirectionRegexPattern = "([0-9]{3}|VRB|///)";
    private const string SpeedRegexPattern = "P?([/0-9]{2,3}|//)";
    private const string SpeedVariationsRegexPattern = "(GP?([0-9]{2,3}))?"; // optional
    private const string UnitRegexPattern = "(KT|MPS|KPH)";
    private const string DirectionVariationsRegexPattern = "( ([0-9]{3})V([0-9]{3}))?"; // optional

    public override string GetRegex()
    {
        return
            $"^{DirectionRegexPattern}{SpeedRegexPattern}{SpeedVariationsRegexPattern}{UnitRegexPattern}{DirectionVariationsRegexPattern}( )";
        //last group capture is here to ensure that array will always have the same size if there is a match
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        // handle the case where nothing has been found
        if (found.Count <= 1)
        {
            throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                MetarChunkDecoderException.Messages.SURFACE_WIND_INFORMATION_BAD_FORMAT);
        }

        // handle the case where nothing is observed
        if (found[1].Value == "///" && found[2].Value == "//")
        {
            throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                MetarChunkDecoderException.Messages.NO_SURFACE_WIND_INFORMATION_MEASURED);
        }

        // get unit used
        Value.Unit speedUnit = found[5].Value switch
        {
            "KT" => Value.Unit.Knot,
            "KPH" => Value.Unit.KilometerPerHour,
            "MPS" => Value.Unit.MeterPerSecond,
            _ => Value.Unit.None
        };

        // retrieve and validate found params
        var surfaceWind = new SurfaceWind
        {
            // mean speed
            MeanSpeed = new Value(Value.ToInt(found[2].Value)!.Value, speedUnit),
            RawValue = found[0].Value
        };

        // mean direction
        if (found[1].Value == "VRB")
        {
            surfaceWind.VariableDirection = true;
            surfaceWind.MeanDirection = null;
        }
        else
        {
            var meanDirection = new Value(Value.ToInt(found[1].Value)!.Value, Value.Unit.Degree);

            if (meanDirection.ActualValue is < 0 or > 360)
            {
                throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                    MetarChunkDecoderException.Messages.INVALID_WIND_DIRECTION_INTERVAL);
            }

            surfaceWind.VariableDirection = false;
            surfaceWind.MeanDirection = meanDirection;
        }

        // direction variations
        if (found[7].Length > 0)
        {
            var minimumDirectionVariation = new Value(Value.ToInt(found[7].Value)!.Value, Value.Unit.Degree);
            var maximumDirectionVariation = new Value(Value.ToInt(found[8].Value)!.Value, Value.Unit.Degree);
            if (minimumDirectionVariation.ActualValue < 0 || minimumDirectionVariation.ActualValue > 360
                                                          || maximumDirectionVariation.ActualValue < 0 ||
                                                          maximumDirectionVariation.ActualValue > 360)
            {
                throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                    MetarChunkDecoderException.Messages.INVALID_WIND_DIRECTION_VARIATIONS_INTERVAL);
            }
            
            surfaceWind.SetDirectionVariations(minimumDirectionVariation, maximumDirectionVariation);
        }

        // speed variations
        if (found[4].Length > 0)
        {
            surfaceWind.SpeedVariations = new Value(Value.ToInt(found[4].Value)!.Value, speedUnit);
        }

        surfaceWind.SpeedUnit = speedUnit;
        
        // retrieve found params
        result.Add(SurfaceWindParameterName, surfaceWind);

        return GetResults(newRemainingMetar, result);
    }
}