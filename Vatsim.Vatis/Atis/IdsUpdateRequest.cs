﻿using System;

namespace Vatsim.Vatis.Atis;

[Serializable]
public class IdsUpdateRequest
{
    public string Facility { get; set; }
    public string Preset { get; set; }
    public string AtisLetter { get; set; }
    public string AirportConditions { get; set; }
    public string Notams { get; set; }
    public DateTime Timestamp { get; set; }
    public string Version { get; set; }
    public string AtisType { get; set; }
}
