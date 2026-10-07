using System.Collections.Generic;
using System.Linq;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Services.Websocket.Messages;
using Xunit;

namespace Vatsim.Vatis.Tests.Websocket;

public class AtisStationMessageTests
{
    private static AtisStation Station(string identifier, int ordinal = 0, AtisType type = AtisType.Combined,
        params (string? Name, int? Ordinal)[] presets)
    {
        return new AtisStation
        {
            Identifier = identifier,
            Name = identifier,
            Ordinal = ordinal,
            AtisType = type,
            Presets = presets.Select(p => new AtisPreset { Name = p.Name, Ordinal = p.Ordinal }).ToList()
        };
    }

    [Fact]
    public void FromStations_WithNull_ReturnsEmptyStationList()
    {
        var message = AtisStationMessage.FromStations(null);

        Assert.Equal("stations", message.MessageType);
        Assert.NotNull(message.Stations);
        Assert.Empty(message.Stations!);
    }

    [Fact]
    public void FromStations_WithNoStations_ReturnsEmptyStationList()
    {
        var message = AtisStationMessage.FromStations([]);

        Assert.NotNull(message.Stations);
        Assert.Empty(message.Stations!);
    }

    [Fact]
    public void FromStations_ReturnsAllStationsWithFullDetails()
    {
        var kden = Station("KDEN", 0, AtisType.Combined, ("Day", 0), ("Night", 1));

        var message = AtisStationMessage.FromStations([kden]);

        var record = Assert.Single(message.Stations!);
        Assert.Equal(kden.Id, record.Id);
        Assert.Equal("KDEN", record.Name);
        Assert.Equal(AtisType.Combined, record.AtisType);
        Assert.Equal(["Day", "Night"], record.Presets);
    }

    [Fact]
    public void FromStations_SkipsStationsWithoutIdOrIdentifier()
    {
        var noId = Station("KAAA");
        noId.Id = string.Empty;
        var noIdentifier = Station("KBBB");
        noIdentifier.Identifier = string.Empty;
        var valid = Station("KCCC");

        var message = AtisStationMessage.FromStations([noId, noIdentifier, valid]);

        var record = Assert.Single(message.Stations!);
        Assert.Equal("KCCC", record.Name);
    }

    [Fact]
    public void FromStations_OrdersByOrdinalThenIdentifierThenType()
    {
        var stations = new List<AtisStation>
        {
            Station("KSFO", 1, AtisType.Departure),
            Station("KSFO", 1, AtisType.Arrival),
            Station("KDEN", 1),
            Station("KZZZ", 0)
        };

        var message = AtisStationMessage.FromStations(stations);

        Assert.Equal(
            [("KZZZ", AtisType.Combined), ("KDEN", AtisType.Combined), ("KSFO", AtisType.Arrival), ("KSFO", AtisType.Departure)],
            message.Stations!.Select(r => (r.Name!, r.AtisType)));
    }

    [Fact]
    public void FromStations_OrdersPresetsByOrdinalThenName_AndDropsUnnamedPresets()
    {
        var station = Station("KDEN", 0, AtisType.Combined,
            ("Zulu", 1), ("Bravo", 0), ("Alpha", 0), (null, 2));

        var message = AtisStationMessage.FromStations([station]);

        Assert.Equal(["Alpha", "Bravo", "Zulu"], Assert.Single(message.Stations!).Presets);
    }
}
