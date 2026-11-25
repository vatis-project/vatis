using System.Collections.Generic;
using Xunit;
using Vatsim.Vatis.TextToSpeech;

namespace Vatsim.Vatis.Tests.TextToSpeech;

public class VoiceMetaDataTests
{
    private static readonly List<VoiceMetaData> Voices =
    [
        new() { Id = "default", Name = "Default" },
        new() { Id = "us-male", Name = "US Male (Nathan)" },
        new() { Id = "uk-male", Name = "UK Male (Malcolm)" },
        new() { Id = "uk-george", Name = "UK Male (George)" },
    ];

    [Theory]
    [InlineData("us-male", "us-male")]
    [InlineData("US-MALE", "us-male")]
    [InlineData("uk-george", "uk-george")]
    [InlineData("US Male (Nathan)", "us-male")]
    [InlineData("US Male", "us-male")]
    [InlineData("Default", "default")]
    [InlineData("Unknown", "default")]
    [InlineData("", "default")]
    [InlineData(null, "default")]
    public void ResolveVoiceId_AcceptsIdsAndLegacyNames(string? saved, string expected)
    {
        Assert.Equal(expected, Voices.ResolveVoiceId(saved));
    }

    [Theory]
    [InlineData("US Male", "us-male")]
    [InlineData("uk female", "uk-female")]
    [InlineData("Default", "default")]
    [InlineData("us-male", "us-male")]
    [InlineData("Something Else", "Something Else")]
    [InlineData(null, null)]
    public void ConvertLegacyVoiceName_ConvertsOnlyKnownNames(string? saved, string? expected)
    {
        Assert.Equal(expected, VoiceMetaDataExtensions.ConvertLegacyVoiceName(saved));
    }
}
