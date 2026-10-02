using System.Linq;
using Vatsim.Vatis.Ui.Common;
using Xunit;

namespace Vatsim.Vatis.Tests.Ui;

public class ContractionUnderlineRendererTests
{
    [Theory]
    [InlineData("[VIS] VIS", new[] { "VIS" })]
    [InlineData("[WIND:VOX] $VIS $VIS:VOX", new string[0])]
    [InlineData("@TWY and [QFE|123] RWY", new[] { "TWY", "and", "RWY" })]
    public void GetContractionCandidates_SkipsTemplateVariables(string line, string[] expected)
    {
        var actual = ContractionUnderlineRenderer.GetContractionCandidates(line).Select(m => m.Groups[1].Value);
        Assert.Equal(expected, actual);
    }
}
