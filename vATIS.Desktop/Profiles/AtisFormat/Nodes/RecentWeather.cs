namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

public class RecentWeather : BaseFormat
{
    public RecentWeather()
    {
        Template = new Template
        {
            Text = "RECENT WEATHER {weather}",
            Voice = "RECENT WEATHER {weather}"
        };
    }
    
    public RecentWeather Clone() => (RecentWeather)MemberwiseClone();
}