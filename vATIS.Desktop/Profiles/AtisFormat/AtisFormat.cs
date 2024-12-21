using Vatsim.Vatis.Profiles.AtisFormat.Nodes;

namespace Vatsim.Vatis.Profiles.AtisFormat;

public class AtisFormat
{
    public ObservationTime ObservationTime { get; set; } = new();
    public SurfaceWind SurfaceWind { get; set; } = new();
    public Visibility Visibility { get; set; } = new();
    public PresentWeather PresentWeather { get; set; } = new();
    public Clouds Clouds { get; set; } = new();
    public Temperature Temperature { get; set; } = new();
    public Dewpoint Dewpoint { get; set; } = new();
    public Altimeter Altimeter { get; set; } = new();
    public TransitionLevel TransitionLevel { get; set; } = new();
    public ClosingStatement ClosingStatement { get; set; } = new();

    public AtisFormat Clone()
    {
        return new()
        {
            ObservationTime = ObservationTime.Clone(),
            SurfaceWind = SurfaceWind.Clone(),
            Visibility = Visibility.Clone(),
            PresentWeather = PresentWeather.Clone(),
            Clouds = Clouds.Clone(),
            Temperature = Temperature.Clone(),
            Dewpoint = Dewpoint.Clone(),
            Altimeter = Altimeter.Clone(),
            TransitionLevel = TransitionLevel.Clone(),
            ClosingStatement = ClosingStatement.Clone()
        };
    }
}