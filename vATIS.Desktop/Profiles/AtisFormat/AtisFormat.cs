using Vatsim.Vatis.Profiles.AtisFormat.Nodes;

namespace Vatsim.Vatis.Profiles.AtisFormat;

public class AtisFormat
{
    public ObservationTime ObservationTime { get; set; } = new();

    public SurfaceWind SurfaceWind { get; set; } = new();

    public Visibility Visibility { get; set; } = new();

    public PresentWeather PresentWeather { get; set; } = new();

    public RecentWeather RecentWeather { get; set; } = new();

    public Clouds Clouds { get; set; } = new();

    public Temperature Temperature { get; set; } = new();

    public Dewpoint Dewpoint { get; set; } = new();

    public Altimeter Altimeter { get; set; } = new();

    public TransitionLevel TransitionLevel { get; set; } = new();

    public Notams Notams { get; set; } = new();

    public ClosingStatement ClosingStatement { get; set; } = new();

    public AtisFormat Clone()
    {
        return new AtisFormat
        {
            ObservationTime = this.ObservationTime.Clone(),
            SurfaceWind = this.SurfaceWind.Clone(),
            Visibility = this.Visibility.Clone(),
            PresentWeather = this.PresentWeather.Clone(),
            RecentWeather = this.RecentWeather.Clone(),
            Clouds = this.Clouds.Clone(),
            Temperature = this.Temperature.Clone(),
            Dewpoint = this.Dewpoint.Clone(),
            Altimeter = this.Altimeter.Clone(),
            TransitionLevel = this.TransitionLevel.Clone(),
            Notams = this.Notams.Clone(),
            ClosingStatement = this.ClosingStatement.Clone()
        };
    }
}