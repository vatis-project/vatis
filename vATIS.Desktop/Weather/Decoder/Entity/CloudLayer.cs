namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents a cloud layer with information regarding its amount, base height, and type.
/// </summary>
public class CloudLayer
{
    /// <summary>
    /// Annotation corresponding to amount of clouds (FEW/SCT/BKN/OVC).
    /// </summary>
    public enum CloudAmount
    {
        /// <summary>
        /// Represents the absence of clouds in the sky. This is a member of the
        /// <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        None,

        /// <summary>
        /// Represents a small amount of clouds in the sky. This is a member of the
        /// <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        Few,

        /// <summary>
        /// Represents a moderate amount of scattered cloud cover in the sky. This is a member of the
        /// <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        Scattered,

        /// <summary>
        /// Represents a significant amount of cloud cover in the sky, typically associated with
        /// broken clouds. This is a member of the <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        Broken,

        /// <summary>
        /// Represents a fully clouded sky with complete overcast conditions. This is a member of the
        /// <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        Overcast,

        /// <summary>
        /// Represents a condition where vertical visibility is reported, typically in cases such as fog or obscuration,
        /// where clouds or sky are not distinctly observable. This is a member of the <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        VerticalVisibility,

        /// <summary>
        /// Represents a condition where no significant clouds are present in the sky. This is a member of the
        /// <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        NoSignificantClouds,

        /// <summary>
        /// Represents a condition where no clouds have been observed in the sky.
        /// This is a member of the <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        NoCloudsDetected,

        /// <summary>
        /// Represents a completely clear sky with no clouds present. This is a member of the
        /// <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        Clear,

        /// <summary>
        /// Represents a completely clear sky with no cloud cover. This is a member of the
        /// <see cref="CloudLayer.CloudAmount"/> enumeration.
        /// </summary>
        SkyClear,
    }

    /// <summary>
    /// Cloud type cumulonimbus, towering cumulonimbus (CB/TCU).
    /// </summary>
    public enum CloudType
    {
        /// <summary>
        /// Represents the absence of a specific cloud type in the sky.
        /// This is a member of the <see cref="CloudLayer.CloudType"/> enumeration.
        /// </summary>
        None,

        /// <summary>
        /// Represents the presence of a cumulonimbus cloud layer, commonly abbreviated as CB.
        /// This is a member of the <see cref="CloudLayer.CloudType"/> enumeration.
        /// </summary>
        Cumulonimbus,

        /// <summary>
        /// Represents the presence of a towering cumulus cloud layer, commonly abbreviated as TCU.
        /// This is a member of the <see cref="CloudLayer.CloudType"/> enumeration.
        /// </summary>
        ToweringCumulus,

        /// <summary>
        /// Represents a condition where the type of clouds cannot be determined or measured.
        /// This is a member of the <see cref="CloudLayer.CloudType"/> enumeration.
        /// </summary>
        CannotMeasure,
    }

    /// <summary>
    /// Gets or sets annotation corresponding to amount of clouds (FEW/SCT/BKN/OVC).
    /// </summary>
    public CloudAmount Amount { get; set; } = CloudAmount.None;

    /// <summary>
    /// Gets or sets the eight of cloud base.
    /// </summary>
    public Value? BaseHeight { get; set; }

    /// <summary>
    /// Gets or sets the type of the cloud layer, such as Cumulonimbus, Towering Cumulus, or None.
    /// </summary>
    public CloudType Type { get; set; } = CloudType.None;
}
