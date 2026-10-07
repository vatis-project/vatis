using System.Collections.Generic;
using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class ObservationTimeNodeTests
{
    private static string ParseText(string rawMetar, List<int>? standardUpdateTime)
    {
        var station = new AtisStation { Identifier = "EFHK", Name = "EFHK" };
        station.AtisFormat.ObservationTime.StandardUpdateTime = standardUpdateTime;
        station.AtisFormat.ObservationTime.Template.Text = "{type}|{special}";

        var node = new ObservationTimeNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict(rawMetar));
        return node.TextAtis!;
    }

    [Fact]
    public void Parse_SpeciReportAtStandardUpdateMinute_IsSpecial()
    {
        Assert.Equal("SPECI|SPECIAL", ParseText("SPECI EFHK 101050Z 05015KT 6000 -SN OVC006 M00/M01 Q1005", [20, 50]));
    }

    [Fact]
    public void Parse_SpeciReportWithoutStandardUpdateTimes_IsSpecial()
    {
        Assert.Equal("SPECI|SPECIAL", ParseText("SPECI EFHK 101105Z 05015KT 6000 -SN OVC006 M00/M01 Q1005", null));
    }

    [Fact]
    public void Parse_SpeciCorReport_IsSpecial()
    {
        Assert.Equal("SPECI|SPECIAL", ParseText("SPECI COR EFHK 101050Z 05015KT 6000 -SN OVC006 M00/M01 Q1005", [20, 50]));
    }

    [Fact]
    public void Parse_MetarReportAtStandardUpdateMinute_IsNotSpecial()
    {
        Assert.Equal("METAR|", ParseText("METAR EFHK 101050Z 05015KT 9999 OVC006 M00/M01 Q1005", [20, 50]));
    }

    [Fact]
    public void Parse_UnprefixedReportOutsideStandardUpdateTimes_IsSpecial()
    {
        Assert.Equal("SPECI|SPECIAL", ParseText("EFHK 101105Z 05015KT 6000 -SN OVC006 M00/M01 Q1005", [20, 50]));
    }

    [Fact]
    public void Parse_UnprefixedReportWithoutStandardUpdateTimes_IsNotSpecial()
    {
        Assert.Equal("METAR|", ParseText("EFHK 101105Z 05015KT 9999 OVC006 M00/M01 Q1005", null));
    }
}
