using System;

namespace Vatsim.Vatis.Profiles.Models;

[Serializable]
public class MagneticVariationMeta
{
    public bool Enabled { get; set; } = false;
    public int MagneticDegrees { get; set; } = 0;
}