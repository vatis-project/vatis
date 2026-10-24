using System.Linq;
using System.Text.Json;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Profiles;

public class StaticDefinitionSerializationTests
{
    [Fact]
    public void BlankLineDefinitions_RoundTripThroughProfile()
    {
        var station = new AtisStation { Identifier = "KABC", Name = "KABC" };
        station.AirportConditionDefinitions.AddRange([new("ONE", 1), new(string.Empty, 2), new("TWO", 3)]);
        station.NotamDefinitions.AddRange([new("ONE", 1), new(string.Empty, 2, enabled: false), new("TWO", 3), new(string.Empty, 4) { NewLineOnly = true }]);
        var profile = new Profile { Name = "Test", Stations = [station] };

        var json = JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile);
        var loaded = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.Profile)!.Stations![0];

        Assert.Equal(["ONE", string.Empty, "TWO"], loaded.AirportConditionDefinitions.Select(x => x.Text));
        Assert.Equal([false, true, false], loaded.AirportConditionDefinitions.Select(x => x.IsBlankLine));
        Assert.True(loaded.AirportConditionDefinitions[1].IsBlankLine);
        Assert.False(loaded.NotamDefinitions[1].Enabled);
        Assert.True(loaded.NotamDefinitions[1].IsBlankLine);
        Assert.False(loaded.NotamDefinitions[1].IsNewLine);
        Assert.True(loaded.NotamDefinitions[3].IsNewLine);
        Assert.False(loaded.NotamDefinitions[3].IsBlankLine);
        Assert.DoesNotContain("IsBlankLine", json);
    }
}
