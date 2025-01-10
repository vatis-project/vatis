using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder;

/// <summary>
/// Metar Decoder
/// </summary>
public class MetarDecoder
{
    public const string ResultKey = "Result";
    public const string RemainingMetarKey = "RemainingMetar";
    private const string ExceptionKey = "Exception";

    /// <summary>
    /// Metar Decoder
    /// </summary>
    public MetarDecoder()
    {

    }

    private static readonly ReadOnlyCollection<MetarChunkDecoder> s_decoderChain = new((List<MetarChunkDecoder>)
    [
        new ReportTypeChunkDecoder(),
        new IcaoChunkDecoder(),
        new DatetimeChunkDecoder(),
        new ReportStatusChunkDecoder(),
        new SurfaceWindChunkDecoder(),
        new VisibilityChunkDecoder(),
        new RunwayVisualRangeChunkDecoder(),
        new PresentWeatherChunkDecoder(),
        new CloudChunkDecoder(),
        new TemperatureChunkDecoder(),
        new PressureChunkDecoder(),
        new RecentWeatherChunkDecoder(),
        new WindShearChunkDecoder(),
        new TrendChunkDecoder()
    ]);

    private bool _globalStrictParsing;

    /// <summary>
    /// Set global parsing mode (strict/not strict) for the whole object.
    /// </summary>
    /// <param name="isStrict"></param>
    public void SetStrictParsing(bool isStrict)
    {
        _globalStrictParsing = isStrict;
    }

    /// <summary>
    /// Decode a full metar string into a complete metar object
    /// while using global strict option.
    /// </summary>
    /// <param name="rawMetar">Metar String</param>
    public DecodedMetar Parse(string rawMetar)
    {
        return ParseWithMode(rawMetar, _globalStrictParsing);
    }

    /// <summary>
    /// Decode a full metar string into a complete metar object
    /// with strict option, meaning decoding will stop as soon as
    /// a non-compliance is detected.
    /// </summary>
    /// <param name="rawMetar"></param>
    /// <returns></returns>
    public DecodedMetar ParseStrict(string rawMetar)
    {
        return ParseWithMode(rawMetar, true);
    }

    /// <summary>
    /// Decode a full metar string into a complete metar object
    /// ith strict option disabled, meaning that decoding will
    /// continue even if metar is not compliant.
    /// </summary>
    /// <param name="rawMetar"></param>
    /// <returns></returns>
    public DecodedMetar ParseNotStrict(string rawMetar)
    {
        return ParseWithMode(rawMetar);
    }

    /// <summary>
    /// Decode a full metar string into a complete metar object.
    /// </summary>
    /// <param name="rawMetar">Metar String</param>
    /// <param name="isStrict">false</param>
    /// <returns></returns>
    private static DecodedMetar ParseWithMode(string rawMetar, bool isStrict = false)
    {
        // prepare decoding inputs/outputs: (upper case, trim,
        // remove 'end of message', no more than one space)
        var cleanMetar = rawMetar.ToUpper().Trim();
        cleanMetar = Regex.Replace(cleanMetar, "=$", string.Empty);
        cleanMetar = Regex.Replace(cleanMetar, "[ ]{2,}", " ") + " ";
        var remainingMetar = cleanMetar;
        var decodedMetar = new DecodedMetar(cleanMetar);
        var withCavok = false;

        // call each decoder in the chain and use results to populate decoded metar
        foreach (var chunkDecoder in s_decoderChain)
        {
            try
            {
                // try to parse a chunk with current chunk decoder
                var decodedData = TryParsing(chunkDecoder, isStrict, remainingMetar ?? "", withCavok);

                // log any exception that would have occurred at primary decoding
                if (decodedData.TryGetValue(ExceptionKey, out var data))
                {
                    decodedMetar.AddDecodingException((MetarChunkDecoderException)data);
                }

                // map obtained fields (if any) to the final decoded object
                if (decodedData.TryGetValue(ResultKey, out var value) && value is Dictionary<string, object>)
                {
                    if (value is Dictionary<string, object> result)
                    {
                        foreach (var obj in result)
                        {
                            typeof(DecodedMetar).GetProperty(obj.Key)?.SetValue(decodedMetar, obj.Value, null);
                        }
                    }
                }

                // update remaining metar for next round
                remainingMetar = decodedData[RemainingMetarKey] as string;
            }
            catch (MetarChunkDecoderException metarChunkDecoderException)
            {
                // log error in decoded metar
                decodedMetar.AddDecodingException(metarChunkDecoderException);
                // abort decoding if strict mode is activated, continue otherwise
                if (isStrict)
                {
                    break;
                }

                // update remaining metar for next round
                remainingMetar = metarChunkDecoderException.RemainingMetar;
            }

            // hook for report status decoder, abort if nil, but decoded metar is valid though
            if (chunkDecoder is ReportStatusChunkDecoder && (decodedMetar.Status == "NIL"))
            {
                break;
            }

            // hook for CAVOK decoder, keep CAVOK information in memory
            if (chunkDecoder is VisibilityChunkDecoder)
            {
                withCavok = decodedMetar.Cavok;
            }
        }

        return decodedMetar;
    }

    private static Dictionary<string, object> TryParsing(IMetarChunkDecoder chunkDecoder, bool strict,
        string remainingMetar, bool withCavok)
    {
        Dictionary<string, object> decoded;
        try
        {
            decoded = chunkDecoder.Parse(remainingMetar, withCavok);
        }
        catch (MetarChunkDecoderException primaryException)
        {
            if (strict)
            {
                throw;
            }

            try
            {
                var alternativeRemainingMetar = MetarChunkDecoder.ConsumeOneChunk(remainingMetar);
                decoded = chunkDecoder.Parse(alternativeRemainingMetar, withCavok);
                decoded.Add(ExceptionKey, primaryException);
            }
            catch (MetarChunkDecoderException)
            {
                throw primaryException;
            }
        }

        return decoded;
    }
}
