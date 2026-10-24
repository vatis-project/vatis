using System.Text.Json;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Services.Websocket.Messages;
using Xunit;

namespace Vatsim.Vatis.Tests.Websocket;

public class ConfigureAtisMessageTests
{
    private static ConfigureAtisMessage.ConfigureAtisMessagePayload Message(string value)
    {
        var json = """{"type":"configureAtis","value":""" + value + "}";
        var message = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.ConfigureAtisMessage);
        Assert.NotNull(message?.Payload);
        return message.Payload;
    }

    [Fact]
    public void ApplyTo_WithOmittedFields_KeepsExistingConditionsAndNotams()
    {
        var preset = new AtisPreset { Name = "D-ATIS", AirportConditions = "RWY 28 CLSD", Notams = "TWY A CLSD" };

        Message("""{"id":"abc","preset":"D-ATIS","syncAtisLetter":true}""").ApplyTo(preset);

        Assert.Equal("RWY 28 CLSD", preset.AirportConditions);
        Assert.Equal("TWY A CLSD", preset.Notams);
    }

    [Fact]
    public void ApplyTo_WithOnlyConditions_LeavesNotamsUntouched()
    {
        var preset = new AtisPreset { Name = "D-ATIS", AirportConditions = "OLD", Notams = "TWY A CLSD" };

        Message("""{"id":"abc","airportConditionsFreeText":"NEW"}""").ApplyTo(preset);

        Assert.Equal("NEW", preset.AirportConditions);
        Assert.Equal("TWY A CLSD", preset.Notams);
    }

    [Fact]
    public void ApplyTo_WithEmptyStrings_ClearsConditionsAndNotams()
    {
        var preset = new AtisPreset { Name = "D-ATIS", AirportConditions = "RWY 28 CLSD", Notams = "TWY A CLSD" };

        Message("""{"id":"abc","airportConditionsFreeText":"","notamsFreeText":""}""").ApplyTo(preset);

        Assert.Equal("", preset.AirportConditions);
        Assert.Equal("", preset.Notams);
    }
}
