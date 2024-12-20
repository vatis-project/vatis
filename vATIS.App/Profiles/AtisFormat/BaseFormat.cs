namespace Vatsim.Vatis.Profiles.AtisFormat;
public class BaseFormat
{
    public Template Template { get; set; } = new Template();
}

public class Template
{
    public string? Text { get; set; }
    public string? Voice { get; set; }
}