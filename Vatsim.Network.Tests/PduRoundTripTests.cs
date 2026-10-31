using Vatsim.Network.PDU;

namespace Vatsim.Network.Tests;

public class PduRoundTripTests
{
    [Fact]
    public void Ping_RoundTrips()
    {
        var pdu = new PDUPing("abc123", "server", "1234567");
        Assert.Equal("$PIABC123:SERVER:1234567", pdu.Serialize());
        var parsed = PDUPing.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("SERVER", parsed.To);
        Assert.Equal("1234567", parsed.TimeStamp);
    }

    [Fact]
    public void Pong_RoundTrips()
    {
        var pdu = new PDUPong("abc123", "server", "1234567");
        Assert.Equal("$POABC123:SERVER:1234567", pdu.Serialize());
        var parsed = PDUPong.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("SERVER", parsed.To);
        Assert.Equal("1234567", parsed.TimeStamp);
    }

    [Fact]
    public void AuthChallenge_RoundTrips()
    {
        var pdu = new PDUAuthChallenge("server", "abc123", "0123abcd");
        var parsed = PDUAuthChallenge.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.StartsWith("$ZC", pdu.Serialize());
        Assert.Equal("SERVER", parsed.From);
        Assert.Equal("ABC123", parsed.To);
        Assert.Equal("0123abcd", parsed.Challenge);
    }

    [Fact]
    public void TextMessage_RoundTrips_PreservingColonsInMessage()
    {
        var pdu = new PDUTextMessage("abc123", "def456", "hello: world: again");
        Assert.Equal("#TMABC123:DEF456:hello: world: again", pdu.Serialize());
        var parsed = PDUTextMessage.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("DEF456", parsed.To);
        Assert.Equal("hello: world: again", parsed.Message);
    }

    [Fact]
    public void ClientQuery_RoundTrips_WithPayload()
    {
        var pdu = new PDUClientQuery("abc123", "def456", ClientQueryType.IsValidAtc, ["X", "Y"]);
        Assert.Equal("$CQABC123:DEF456:ATC:X:Y", pdu.Serialize());
        var parsed = PDUClientQuery.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal(ClientQueryType.IsValidAtc, parsed.QueryType);
        Assert.Equal(["X", "Y"], parsed.Payload);
    }

    [Fact]
    public void ClientQuery_RoundTrips_WithoutPayload()
    {
        var pdu = new PDUClientQuery("abc123", "def456", ClientQueryType.Capabilities);
        var parsed = PDUClientQuery.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal(ClientQueryType.Capabilities, parsed.QueryType);
        Assert.Empty(parsed.Payload!);
    }

    [Fact]
    public void AddPilot_RoundTrips()
    {
        var pdu = new PDUAddPilot("abc123", "1234567", "secret", NetworkRating.S2, default, default, "Jane Doe");
        var parsed = PDUAddPilot.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("1234567", parsed.CID);
        Assert.Equal("secret", parsed.Password);
        Assert.Equal(NetworkRating.S2, parsed.Rating);
        Assert.Equal(pdu.ProtocolRevision, parsed.ProtocolRevision);
        Assert.Equal(pdu.SimulatorType, parsed.SimulatorType);
        Assert.Equal("Jane Doe", parsed.RealName);
    }

    [Fact]
    public void DeletePilot_RoundTrips_AndCidIsOptional()
    {
        var pdu = new PDUDeletePilot("abc123", "1234567");
        Assert.Equal("1234567", PDUDeletePilot.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3)).CID);
        Assert.Equal("", PDUDeletePilot.Parse(["ABC123"]).CID);
    }

    [Theory]
    [InlineData(false, false, "S")]
    [InlineData(true, false, "N")]
    [InlineData(true, true, "Y")]
    public void PilotPosition_RoundTrips(bool modeC, bool ident, string mode)
    {
        var pdu = new PDUPilotPosition("abc123", 1200, modeC, ident, NetworkRating.Obs, 47.4489, -122.3094,
            12000, 12400, 250, 5.0, 90.0, -10.0);
        var serialized = pdu.Serialize();
        Assert.StartsWith($"@{mode}:ABC123:1200:1:47.4489000:-122.3094000:12000:250:", serialized);
        Assert.EndsWith(":400", serialized);

        var parsed = PDUPilotPosition.Parse(PduTestHelpers.Fields(serialized, 1));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal(1200, parsed.SquawkCode);
        Assert.Equal(modeC || ident, parsed.IsSquawkingModeC);
        Assert.Equal(ident, parsed.IsIdenting);
        Assert.Equal(NetworkRating.Obs, parsed.Rating);
        Assert.Equal(47.4489, parsed.Lat, 6);
        Assert.Equal(-122.3094, parsed.Lon, 6);
        Assert.Equal(12000, parsed.TrueAltitude);
        Assert.Equal(12400, parsed.PressureAltitude);
        Assert.Equal(250, parsed.GroundSpeed);
        Assert.InRange(parsed.Pitch, 5.0 - 0.36, 5.0 + 0.36);
        Assert.InRange(parsed.Heading, 90.0 - 0.36, 90.0 + 0.36);
        Assert.InRange(parsed.Bank, -10.0 - 0.36, -10.0 + 0.36);
    }

    [Fact]
    public void PilotPosition_SquawkCodeIsZeroPadded() =>
        Assert.Contains(":0007:", new PDUPilotPosition("A", 7, true, false, NetworkRating.Obs, 0, 0, 0, 0, 0, 0, 0, 0).Serialize());

    [Fact]
    public void PilotPosition_NaNLatOrLon_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new PDUPilotPosition("A", 1200, true, false, NetworkRating.Obs, double.NaN, 0, 0, 0, 0, 0, 0, 0));
        Assert.Throws<ArgumentException>(() =>
            new PDUPilotPosition("A", 1200, true, false, NetworkRating.Obs, 0, double.NaN, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void PilotPosition_MalformedNumber_ThrowsFormatExceptionWithRaw()
    {
        var ex = Assert.Throws<PDUFormatException>(() =>
            PDUPilotPosition.Parse(["N", "ABC", "1200", "1", "xx", "0", "0", "0", "0", "0"]));
        Assert.Equal("N:ABC:1200:1:xx:0:0:0:0:0", ex.RawMessage);
    }

    [Fact]
    public void FlightPlan_RoundTrips()
    {
        var pdu = new PDUFlightPlan("abc123", "server", FlightRules.Ifr, "B738/L", "420", "KJFK", "1200", "1205",
            "35000", "KLAX", "5", "30", "6", "0", "KLAS", "test remarks", "KJFK.TEST1.KLAX");
        Assert.Equal(
            "$FPABC123:SERVER:I:B738/L:420:KJFK:1200:1205:35000:KLAX:5:30:6:0:KLAS:test remarks:KJFK.TEST1.KLAX",
            pdu.Serialize());

        var parsed = PDUFlightPlan.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal(FlightRules.Ifr, parsed.Rules);
        Assert.Equal("B738/L", parsed.Equipment);
        Assert.Equal("420", parsed.TAS);
        Assert.Equal("KJFK", parsed.DepAirport);
        Assert.Equal("1200", parsed.EstimatedDepTime);
        Assert.Equal("1205", parsed.ActualDepTime);
        Assert.Equal("35000", parsed.CruiseAlt);
        Assert.Equal("KLAX", parsed.DestAirport);
        Assert.Equal("5", parsed.HoursEnroute);
        Assert.Equal("30", parsed.MinutesEnroute);
        Assert.Equal("6", parsed.FuelAvailHours);
        Assert.Equal("0", parsed.FuelAvailMinutes);
        Assert.Equal("KLAS", parsed.AltAirport);
        Assert.Equal("test remarks", parsed.Remarks);
        Assert.Equal("KJFK.TEST1.KLAX", parsed.Route);
    }

    [Fact]
    public void FlightPlan_RemarksAndRouteColonsAreReplacedWithSpaces()
    {
        var pdu = new PDUFlightPlan("a", "b", FlightRules.Vfr, "", "", "", "", "", "", "", "", "", "", "", "",
            "RMK/TEST:VALUE", "KJFK..KLAX:VIA:FOO");
        Assert.Equal("RMK/TEST VALUE", pdu.Remarks);
        Assert.Equal("KJFK..KLAX VIA FOO", pdu.Route);
    }

    [Fact]
    public void FlightPlanAmendment_RoundTrips()
    {
        var pdu = new PDUFlightPlanAmendment("abc123", "server", "UAL123", FlightRules.Vfr, "C172", "110", "KBFI",
            "1400", "", "3500", "KPAE", "0", "20", "2", "30", "KRNT", "remarks", "KBFI.KPAE");
        Assert.Equal(
            "$AMABC123:SERVER:UAL123:V:C172:110:KBFI:1400::3500:KPAE:0:20:2:30:KRNT:remarks:KBFI.KPAE",
            pdu.Serialize());

        var parsed = PDUFlightPlanAmendment.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("UAL123", parsed.Callsign);
        Assert.Equal(FlightRules.Vfr, parsed.Rules);
        Assert.Equal("C172", parsed.Equipment);
        Assert.Equal("KBFI", parsed.DepAirport);
        Assert.Equal("KPAE", parsed.DestAirport);
        Assert.Equal("KRNT", parsed.AltAirport);
        Assert.Equal("remarks", parsed.Remarks);
        Assert.Equal("KBFI.KPAE", parsed.Route);
    }

    [Fact]
    public void AtcMessage_RoundTrips_PreservingColonsInMessage()
    {
        var pdu = new PDUATCMessage("abc123", "hello: atc: message");
        Assert.Equal("#TMABC123:@49999:hello: atc: message", pdu.Serialize());
        var parsed = PDUATCMessage.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("hello: atc: message", parsed.Message);
    }

    [Fact]
    public void BroadcastMessage_RoundTrips_PreservingColonsInMessage()
    {
        var pdu = new PDUBroadcastMessage("abc123", "hello: everyone");
        Assert.Equal("#TMABC123:*:hello: everyone", pdu.Serialize());
        var parsed = PDUBroadcastMessage.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("hello: everyone", parsed.Message);
    }

    [Fact]
    public void Wallop_RoundTrips_PreservingColonsInMessage()
    {
        var pdu = new PDUWallop("abc123", "hello: supervisors");
        Assert.Equal("#TMABC123:*S:hello: supervisors", pdu.Serialize());
        var parsed = PDUWallop.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("hello: supervisors", parsed.Message);
    }

    [Fact]
    public void RadioMessage_RoundTrips_WithMultipleFrequenciesAndColonsInMessage()
    {
        var pdu = new PDURadioMessage("abc123", [121500, 122800], "hello: pilots");
        Assert.Equal("#TMABC123:@121500&@122800:hello: pilots", pdu.Serialize());

        var parsed = PDURadioMessage.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal([121500, 122800], parsed.Frequencies);
        Assert.Equal("hello: pilots", parsed.Message);
    }

    [Fact]
    public void Handoff_RoundTrips()
    {
        var pdu = new PDUHandoff("abc123", "def456", "UAL123");
        Assert.Equal("$HOABC123:DEF456:UAL123", pdu.Serialize());
        var parsed = PDUHandoff.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("DEF456", parsed.To);
        Assert.Equal("UAL123", parsed.Target);
    }

    [Fact]
    public void HandoffAccept_RoundTrips()
    {
        var pdu = new PDUHandoffAccept("abc123", "def456", "UAL123");
        Assert.Equal("$HAABC123:DEF456:UAL123", pdu.Serialize());
        var parsed = PDUHandoffAccept.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("UAL123", parsed.Target);
    }

    [Fact]
    public void HandoffCancelled_RoundTrips()
    {
        var pdu = new PDUHandoffCancelled("abc123", "def456", "UAL123");
        Assert.Equal("#PCABC123:DEF456:CCP:HC:UAL123", pdu.Serialize());
        var parsed = PDUHandoffCancelled.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("DEF456", parsed.To);
        Assert.Equal("UAL123", parsed.Target);
    }

    [Theory]
    [InlineData(SharedStateType.Scratchpad, "SC")]
    [InlineData(SharedStateType.BeaconCode, "BC")]
    [InlineData(SharedStateType.VoiceType, "VT")]
    [InlineData(SharedStateType.TempAlt, "TA")]
    [InlineData(SharedStateType.GlobalData, "GD")]
    public void SharedState_RoundTrips(SharedStateType type, string code)
    {
        var pdu = new PDUSharedState("abc123", "def456", type, "UAL123", "1200");
        Assert.Equal($"#PCABC123:DEF456:CCP:{code}:UAL123:1200", pdu.Serialize());

        var parsed = PDUSharedState.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal(type, parsed.SharedStateType);
        Assert.Equal("UAL123", parsed.Target);
        Assert.Equal("1200", parsed.Value);
    }

    [Fact]
    public void WindData_RoundTrips()
    {
        var pdu = new PDUWindData("abc123", "server",
            2000, 0, 270, 15, true, 1,
            8000, 2000, 280, 25, false, 2,
            18000, 8000, 300, 40, true, 3,
            35000, 18000, 320, 60, false, 4);
        var parsed = PDUWindData.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));

        Assert.Equal(2000, parsed.Layer1Ceiling);
        Assert.Equal(0, parsed.Layer1Floor);
        Assert.Equal(270, parsed.Layer1Direction);
        Assert.Equal(15, parsed.Layer1Speed);
        Assert.True(parsed.Layer1Gusting);
        Assert.Equal(1, parsed.Layer1Turbulence);

        Assert.Equal(8000, parsed.Layer2Ceiling);
        Assert.False(parsed.Layer2Gusting);
        Assert.Equal(2, parsed.Layer2Turbulence);

        Assert.Equal(18000, parsed.Layer3Ceiling);
        Assert.True(parsed.Layer3Gusting);

        Assert.Equal(35000, parsed.Layer4Ceiling);
        Assert.Equal(18000, parsed.Layer4Floor);
        Assert.Equal(320, parsed.Layer4Direction);
        Assert.Equal(60, parsed.Layer4Speed);
        Assert.False(parsed.Layer4Gusting);
        Assert.Equal(4, parsed.Layer4Turbulence);
    }

    [Fact]
    public void TemperatureData_RoundTrips()
    {
        var pdu = new PDUTemperatureData("abc123", "server", 0, 15, 5000, 10, 10000, 0, 20000, -10, 2992);
        var parsed = PDUTemperatureData.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));

        Assert.Equal(0, parsed.Layer1Ceiling);
        Assert.Equal(15, parsed.Layer1Temperature);
        Assert.Equal(5000, parsed.Layer2Ceiling);
        Assert.Equal(10, parsed.Layer2Temperature);
        Assert.Equal(10000, parsed.Layer3Ceiling);
        Assert.Equal(0, parsed.Layer3Temperature);
        Assert.Equal(20000, parsed.Layer4Ceiling);
        Assert.Equal(-10, parsed.Layer4Temperature);
        Assert.Equal(2992, parsed.Pressure);
    }

    [Fact]
    public void CloudData_RoundTrips()
    {
        var pdu = new PDUCloudData("abc123", "server",
            2000, 500, 3, true, 1,
            10000, 5000, 5, false, 2,
            25000, 15000, 45, 8, 3);
        var parsed = PDUCloudData.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));

        Assert.Equal(2000, parsed.Layer1Ceiling);
        Assert.Equal(500, parsed.Layer1Floor);
        Assert.Equal(3, parsed.Layer1Coverage);
        Assert.True(parsed.Layer1Icing);
        Assert.Equal(1, parsed.Layer1Turbulence);

        Assert.Equal(10000, parsed.Layer2Ceiling);
        Assert.False(parsed.Layer2Icing);

        Assert.Equal(25000, parsed.StormLayerCeiling);
        Assert.Equal(15000, parsed.StormLayerFloor);
        Assert.Equal(45, parsed.StormLayerDeviation);
        Assert.Equal(8, parsed.StormLayerCoverage);
        Assert.Equal(3, parsed.StormLayerTurbulence);
    }

    [Fact]
    public void MetarRequest_RoundTrips()
    {
        var pdu = new PDUMetarRequest("abc123", "KJFK");
        Assert.Equal("$AXABC123:SERVER:METAR:KJFK", pdu.Serialize());
        var parsed = PDUMetarRequest.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("KJFK", parsed.Station);
    }

    [Fact]
    public void MetarResponse_RoundTrips()
    {
        // Mirrors PDUMetarRequest's wire format: a "METAR" tag precedes the payload, matching what
        // FSDSession actually parses from a real server's $AR packet.
        var pdu = new PDUMetarResponse("abc123", "KJFK 251853Z 18010KT 10SM FEW250 24/18 A2992");
        Assert.Equal("$ARSERVER:ABC123:METAR:KJFK 251853Z 18010KT 10SM FEW250 24/18 A2992", pdu.Serialize());

        var parsed = PDUMetarResponse.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.To);
        Assert.Equal("KJFK 251853Z 18010KT 10SM FEW250 24/18 A2992", parsed.Metar);
    }

    [Fact]
    public void PlaneInfoRequest_RoundTrips()
    {
        var pdu = new PDUPlaneInfoRequest("abc123", "def456");
        Assert.Equal("#SBABC123:DEF456:PIR", pdu.Serialize());
        var parsed = PDUPlaneInfoRequest.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("DEF456", parsed.To);
    }

    [Fact]
    public void PlaneInfoResponse_RoundTrips_WithAllFields()
    {
        var pdu = new PDUPlaneInfoResponse("abc123", "def456", "B738", "UAL", "united2019", "b738_ual");
        var parsed = PDUPlaneInfoResponse.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("B738", parsed.Equipment);
        Assert.Equal("UAL", parsed.Airline);
        Assert.Equal("united2019", parsed.Livery);
        Assert.Equal("b738_ual", parsed.CSL);
    }

    [Fact]
    public void PlaneInfoResponse_RoundTrips_WithOnlyEquipment()
    {
        var pdu = new PDUPlaneInfoResponse("abc123", "def456", "B738", "", "", "");
        Assert.Equal("#SBABC123:DEF456:PI:GEN:EQUIPMENT=B738", pdu.Serialize());

        var parsed = PDUPlaneInfoResponse.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal("B738", parsed.Equipment);
        Assert.Equal("", parsed.Airline);
        Assert.Equal("", parsed.Livery);
        Assert.Equal("", parsed.CSL);
    }

    [Fact]
    public void LegacyPlaneInfoResponse_RoundTrips()
    {
        var pdu = new PDULegacyPlaneInfoResponse("abc123", "def456", EngineType.Jet, "b738_ual");
        Assert.Equal("#SBABC123:DEF456:PI:X:0:1:CSL=b738_ual", pdu.Serialize());

        var parsed = PDULegacyPlaneInfoResponse.Parse(PduTestHelpers.Fields(pdu.Serialize(), 3));
        Assert.Equal(EngineType.Jet, parsed.EngineType);
        Assert.Equal("CSL=b738_ual", parsed.CSL);
    }

    [Fact]
    public void LegacyPlaneInfoResponse_Parse_SixFieldsIsTooFew()
    {
        // The CSL field (fields[6]) makes 7 the true minimum; this must fail with the same
        // "Invalid field count." message as any other too-short packet, not a wrapped parse error.
        var ex = Assert.Throws<PDUFormatException>(
            () => PDULegacyPlaneInfoResponse.Parse(["ABC123", "DEF456", "PI", "X", "0", "1"]));
        Assert.Equal("Invalid field count.", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Theory]
    [MemberData(nameof(ShortFieldParsers))]
    public void Parse_TooFewFields_ThrowsPduFormatException(string name, Action<string[]> parse, int minFields)
    {
        _ = name;
        var fields = Enumerable.Range(0, minFields - 1).Select(i => i.ToString()).ToArray();
        var ex = Assert.Throws<PDUFormatException>(() => parse(fields));
        Assert.Equal("Invalid field count.", ex.Message);
    }

    public static TheoryData<string, Action<string[]>, int> ShortFieldParsers() => new()
    {
        { nameof(PDUPing), f => PDUPing.Parse(f), 3 },
        { nameof(PDUPong), f => PDUPong.Parse(f), 3 },
        { nameof(PDUAuthChallenge), f => PDUAuthChallenge.Parse(f), 3 },
        { nameof(PDUTextMessage), f => PDUTextMessage.Parse(f), 3 },
        { nameof(PDUClientQuery), f => PDUClientQuery.Parse(f), 3 },
        { nameof(PDUAddPilot), f => PDUAddPilot.Parse(f), 8 },
        { nameof(PDUPilotPosition), f => PDUPilotPosition.Parse(f), 10 },
        { nameof(PDUBroadcastMessage), f => PDUBroadcastMessage.Parse(f), 3 },
        { nameof(PDUWallop), f => PDUWallop.Parse(f), 3 },
        { nameof(PDUATCMessage), f => PDUATCMessage.Parse(f), 3 },
        { nameof(PDURadioMessage), f => PDURadioMessage.Parse(f), 3 },
        { nameof(PDUFlightPlan), f => PDUFlightPlan.Parse(f), 17 },
        { nameof(PDUFlightPlanAmendment), f => PDUFlightPlanAmendment.Parse(f), 18 },
        { nameof(PDUHandoff), f => PDUHandoff.Parse(f), 3 },
        { nameof(PDUHandoffAccept), f => PDUHandoffAccept.Parse(f), 3 },
        { nameof(PDUHandoffCancelled), f => PDUHandoffCancelled.Parse(f), 5 },
        { nameof(PDUSharedState), f => PDUSharedState.Parse(f), 6 },
        { nameof(PDUWindData), f => PDUWindData.Parse(f), 26 },
        { nameof(PDUTemperatureData), f => PDUTemperatureData.Parse(f), 11 },
        { nameof(PDUCloudData), f => PDUCloudData.Parse(f), 17 },
        { nameof(PDUMetarRequest), f => PDUMetarRequest.Parse(f), 4 },
        { nameof(PDUMetarResponse), f => PDUMetarResponse.Parse(f), 4 },
        { nameof(PDUPlaneInfoRequest), f => PDUPlaneInfoRequest.Parse(f), 3 },
        { nameof(PDUPlaneInfoResponse), f => PDUPlaneInfoResponse.Parse(f), 5 },
        { nameof(PDULegacyPlaneInfoResponse), f => PDULegacyPlaneInfoResponse.Parse(f), 7 },
    };
}
