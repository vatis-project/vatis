namespace Vatsim.Vatis.Weather.Decoder.Entity;

public class CloudLayer
{
    /// <summary>
    /// Annotation corresponding to amount of clouds (FEW/SCT/BKN/OVC)
    /// </summary>
    public enum CloudAmount
    {
        None,
        Few,
        Scattered,
        Broken,
        Overcast,
        VerticalVisibility,
        NoSignificantClouds,
        NoCloudsDetected,
        Clear,
        SkyClear 
    }

    /// <summary>
    /// Cloud type cumulonimbus, towering cumulonimbus (CB/TCU)
    /// </summary>
    public enum CloudType
    {
        None,
        Cumulonimbus,
        ToweringCumulus, 
        CannotMeasure,
    }

    /// <summary>
    /// Annotation corresponding to amount of clouds (FEW/SCT/BKN/OVC)
    /// </summary>
    public CloudAmount Amount { get; set; } = CloudAmount.None;

    /// <summary>
    /// Height of cloud base
    /// </summary>
    public Value? BaseHeight { get; set; }

    /// <summary>
    /// Cloud type cumulonimbus, towering cumulonimbus (CB/TCU)
    /// </summary>
    public CloudType Type { get; set; } = CloudType.None;
}