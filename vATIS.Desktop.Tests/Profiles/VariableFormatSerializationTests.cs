using System.Text.Json;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Profiles;

public class VariableFormatSerializationTests
{
    [Fact]
    public void VariableFormats_RoundTripThroughSourceGeneratedContext()
    {
        var profile = new Profile { Name = "Test", Stations = [new AtisStation { Identifier = "KABC", Name = "KABC" }] };
        var format = profile.Stations[0].AtisFormat;
        format.Altimeter.VariableFormats["hpa"].Voice.Style = NumberStyle.Group;
        format.Altimeter.VariableFormats["hpa"].Text.Suffix = " HPA";
        format.Temperature.Format.Voice.Suffix = " degrees";

        var json = JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile);
        Assert.Contains("\"Group\"", json);

        var loaded = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.Profile)!;
        var loadedFormat = loaded.Stations![0].AtisFormat;
        Assert.Equal(NumberStyle.Group, loadedFormat.Altimeter.VariableFormats["hpa"].Voice.Style);
        Assert.Equal(" HPA", loadedFormat.Altimeter.VariableFormats["hpa"].Text.Suffix);
        Assert.Equal(" degrees", loadedFormat.Temperature.Format.Voice.Suffix);
    }

    [Fact]
    public void LegacyProfile_WithoutVariableFormats_GetsDefaults()
    {
        var altimeter = JsonSerializer.Deserialize<Altimeter>("""{"PronounceDecimal":true}""");
        Assert.NotNull(altimeter);
        Assert.Equal(NumberStyle.Default, altimeter!.VariableFormats["hpa"].Voice.Style);
    }
}
