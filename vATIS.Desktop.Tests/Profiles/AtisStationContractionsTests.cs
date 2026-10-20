using System.Collections.Generic;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Profiles;

public class AtisStationContractionsTests
{
    [Fact]
    public void Contractions_GeneratesMissingVariableNamesOnlyForPlainContractions()
    {
        var station = new AtisStation
        {
            Id = "1",
            Name = "Test",
            Identifier = "KTST",
            Contractions =
            [
                new ContractionMeta { Text = "RNAV APPS", Voice = "ARE NAV APPROACHES" },
                new ContractionMeta { IsRegex = true, Pattern = @"(\d+)FT", Text = "$1FT", Voice = "$group($1) feet" },
            ],
        };

        Assert.Equal("RNAV_APPS", station.Contractions[0].VariableName);
        Assert.Null(station.Contractions[1].VariableName);
    }
}
