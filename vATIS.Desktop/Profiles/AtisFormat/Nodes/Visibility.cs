using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;
public class Visibility : BaseFormat
{
    public Visibility()
    {
        Template = new()
        {
            Text = "{visibility}",
            Voice = "VISIBILITY {visibility}"
        };
    }
    public string North { get; set; } = "to the north";
    public string NorthEast { get; set; } = "to the north-east";
    public string East { get; set; } = "to the east";
    public string SouthEast { get; set; } = "to the south-east";
    public string South { get; set; } = "to the south";
    public string SouthWest { get; set; } = "to the south-west";
    public string West { get; set; } = "to the west";
    public string NorthWest { get; set; } = "to the north-west";
    public string UnlimitedVisibilityVoice { get; set; } = "visibility 10 kilometers or more";
    public string UnlimitedVisibilityText { get; set; } = "VIS 10KM";
    public bool IncludeVisibilitySuffix { get; set; } = true;
    public int MetersCutoff { get; set; } = 5000;

    [JsonPropertyName("UnlimitedVisibility")]
    private string UnlimitedVisibility { set => UnlimitedVisibilityVoice = value; }

    public Visibility Clone() => (Visibility)MemberwiseClone();
}
