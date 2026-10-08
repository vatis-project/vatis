using System.Text.Json;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Services.Websocket.Messages;
using Xunit;

namespace Vatsim.Vatis.Tests.Websocket;

public class ConnectionErrorMessageTests
{
    [Fact]
    public void FromStation_CarriesTheStationReasonAndMessage()
    {
        var station = new AtisStation { Identifier = "KSFO", Name = "KSFO", AtisType = AtisType.Departure };

        var message = ConnectionErrorMessage.FromStation(station, ConnectionErrorReason.NetworkError,
            "ATIS callsign already in use.");

        Assert.Equal("connectionError", message.MessageType);
        Assert.NotNull(message.Value);
        Assert.Equal(station.Id, message.Value.Id);
        Assert.Equal("KSFO", message.Value.Station);
        Assert.Equal(AtisType.Departure, message.Value.AtisType);
        Assert.Equal(ConnectionErrorReason.NetworkError, message.Value.Reason);
        Assert.Equal("ATIS callsign already in use.", message.Value.Message);
    }

    [Fact]
    public void Serialize_WritesTheDocumentedShape()
    {
        var station = new AtisStation { Identifier = "KDEN", Name = "KDEN", AtisType = AtisType.Combined };
        var message = ConnectionErrorMessage.FromStation(station, ConnectionErrorReason.TooManyConnections,
            "You've exceeded the maximum number of allowed ATIS connections.");

        var json = JsonSerializer.Serialize(message, SourceGenerationContext.NewDefault.ConnectionErrorMessage);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("connectionError", root.GetProperty("type").GetString());
        var value = root.GetProperty("value");
        Assert.Equal(station.Id, value.GetProperty("id").GetString());
        Assert.Equal("KDEN", value.GetProperty("station").GetString());
        Assert.Equal("Combined", value.GetProperty("atisType").GetString());
        Assert.Equal("TooManyConnections", value.GetProperty("reason").GetString());
        Assert.Equal("You've exceeded the maximum number of allowed ATIS connections.",
            value.GetProperty("message").GetString());
    }
}
