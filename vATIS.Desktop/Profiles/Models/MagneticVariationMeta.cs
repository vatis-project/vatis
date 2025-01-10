using System;

namespace Vatsim.Vatis.Profiles.Models;

[Serializable]
public class MagneticVariationMeta
{
    public bool Enabled { get; set; }
    public int MagneticDegrees { get; set; }
}
