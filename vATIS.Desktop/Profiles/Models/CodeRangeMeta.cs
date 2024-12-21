namespace Vatsim.Vatis.Profiles.Models;

public class CodeRangeMeta
{
    public char Low { get; set; }
    public char High { get; set; }

    public CodeRangeMeta(char low, char high)
    {
        Low = low;
        High = high;
    }
}