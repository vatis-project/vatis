using System.Threading.Tasks;
using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis;

public static class NodeParser
{
    public static ParsedResult Parse<T, TU>(DecodedMetar metar, AtisStation station) where T : BaseNode<TU>, new()
    {
        var obj = new T
        {
            Station = station
        };
        obj.Parse(metar);

        return new ParsedResult
        {
            TextAtis = obj.TextAtis ?? "",
            VoiceAtis = $"{obj.VoiceAtis}."
        };
    }

    public static async Task<ParsedResult> Parse<T, TU>(
        DecodedMetar metar,
        AtisStation station,
        IMetarRepository metarRepository)
        where T : BaseNodeMetarRepository<TU>, new()
    {
        var obj = new T
        {
            Station = station
        };
        await obj.Parse(metar, metarRepository);

        return new ParsedResult
        {
            TextAtis = obj.TextAtis ?? "",
            VoiceAtis = $"{obj.VoiceAtis}."
        };
    }
}

public class ParsedResult
{
    public required string TextAtis { get; init; }

    public required string VoiceAtis { get; init; }
}