using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;
public abstract class BaseNode<T>
{
    public abstract void Parse(DecodedMetar metar);
    public AtisStation? Station { get; init; }
    public string? VoiceAtis { get; protected set; }
    public string? TextAtis { get; protected set; }
    public abstract string ParseVoiceVariables(T node, string? format);
    public abstract string ParseTextVariables(T value, string? format);
}

public abstract class BaseNodeMetarRepository<T> : BaseNode<T>
{
    public abstract Task Parse(DecodedMetar metar, IMetarRepository metarRepository);
}
