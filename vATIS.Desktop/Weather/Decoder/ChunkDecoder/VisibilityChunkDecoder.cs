using System;
using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Provides functionality to decode a specific chunk of a METAR related to visibility information.
/// </summary>
/// <remarks>
/// Decodes visibility information from METAR reports, including CAVOK indications, visibility distance,
/// and other relevant visibility-related data. This class extends <see cref="MetarChunkDecoder"/> to provide
/// specialized decoding functionalities for visibility-related chunks in METAR weather data.
/// </remarks>
public sealed class VisibilityChunkDecoder : MetarChunkDecoder
{
    private const string CavokParameterName = "Cavok";
    private const string VisibilityParameterName = "Visibility";
    private const string CavokRegexPattern = "CAVOK";
    private const string VisibilityRegexPattern = "([0-9]{4})(NDV)?";
    private const string UsVisibilityRegexPattern = "M?([0-9]{0,2}) ?(([1357])/(2|4|8|16))?SM";
    private const string MinimumVisibilityRegexPattern = "( ([0-9]{4})(N|NE|E|SE|S|SW|W|NW)?)?"; // optional
    private const string NoInfoRegexPattern = "////";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        return
            $"^({CavokRegexPattern}|{VisibilityRegexPattern}{MinimumVisibilityRegexPattern}|{UsVisibilityRegexPattern}|{NoInfoRegexPattern})( )";
    }

    /// <inheritdoc/>
    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = this.Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        // handle the case where nothing has been found
        if (found.Count <= 1)
        {
            throw new MetarChunkDecoderException(
                remainingMetar,
                newRemainingMetar,
                MetarChunkDecoderException.Messages.ForVisibilityInformationBadFormat);
        }

        var visibility = new Visibility();
        bool cavok;
        if (found[1].Value == CavokRegexPattern)
        {
            // cloud and visibility OK
            cavok = true;
            visibility.IsCavok = cavok;
        }
        else if (found[1].Value == "////")
        {
            // information not available
            cavok = false;
            visibility.IsCavok = cavok;
        }
        else
        {
            cavok = false;

            visibility.RawValue = found[1].Value;
            visibility.IsCavok = cavok;

            if (!string.IsNullOrEmpty(found[2].Value))
            {
                // icao visibility
                visibility.PrevailingVisibility = new Value(Convert.ToDouble(found[2].Value), Value.Unit.Meter);
                if (!string.IsNullOrEmpty(found[4].Value))
                {
                    visibility.MinimumVisibility = new Value(Convert.ToDouble(found[5].Value), Value.Unit.Meter);
                    visibility.MinimumVisibilityDirection = found[6].Value;
                }

                visibility.IsNdv = !string.IsNullOrEmpty(found[3].Value);
            }
            else
            {
                // us visibility
                double visibilityValue = 0;
                if (!string.IsNullOrEmpty(found[7].Value))
                {
                    visibilityValue += Convert.ToInt32(found[7].Value);
                }

                if (!string.IsNullOrEmpty(found[9].Value) && !string.IsNullOrEmpty(found[10].Value))
                {
                    var fractionTop = Convert.ToInt32(found[9].Value);
                    var fractionBottom = Convert.ToInt32(found[10].Value);
                    if (fractionBottom != 0)
                    {
                        visibilityValue += (double)fractionTop / fractionBottom;
                    }
                }

                visibility.PrevailingVisibility = new Value(visibilityValue, Value.Unit.StatuteMile);
                visibility.IsCavok = cavok;
            }
        }

        result.Add(CavokParameterName, cavok);
        result.Add(VisibilityParameterName, visibility);

        return this.GetResults(newRemainingMetar, result);
    }
}
