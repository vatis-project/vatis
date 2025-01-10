namespace Vatsim.Vatis.Profiles.Models;

public class CodeRangeMeta
{
    public CodeRangeMeta(char low, char high)
    {
        this.Low = low;
        this.High = high;
    }

    public char Low { get; set; }

    public char High { get; set; }
}