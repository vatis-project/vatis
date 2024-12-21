namespace Vatsim.Vatis.Weather.Decoder.Entity;

public sealed class TrendForecast
{
    public TrendForecastType ChangeIndicator { get; set; } = TrendForecastType.None;
    public string? FromTime { get; set; }
    public string? UntilTime { get; set; }
    public string? AtTime { get; set; }
    public string? Forecast { get; set; }
}

public enum TrendForecastType
{
    None,
    Becoming,
    Temporary,
    NoSignificantChanges
}