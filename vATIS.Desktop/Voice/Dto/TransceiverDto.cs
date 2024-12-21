using MessagePack;

namespace Vatsim.Vatis.Voice.Dto;

[MessagePackObject]
public class TransceiverDto
{
    [Key(0)]
    public ushort ID { get; set; }
    [Key(1)]
    public uint Frequency { get; set; }
    [Key(2)]
    public double LatDeg { get; set; }
    [Key(3)]
    public double LonDeg { get; set; }
    [Key(4)]
    public double HeightMslM { get; set; }
    [Key(5)]
    public double HeightAglM { get; set; }
}