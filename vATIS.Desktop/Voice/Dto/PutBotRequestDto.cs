using System;
using System.Collections.Generic;

namespace Vatsim.Vatis.Voice.Dto;
public class PutBotRequestDto
{
    public List<TransceiverDto>? Transceivers { get; set; }
    public TimeSpan Interval { get; set; }
    public List<byte[]>? OpusData { get; set; }
}
