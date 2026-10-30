using Vatsim.Network.PDU;

namespace Vatsim.Network.Tests;

/// <summary>Round-trip and wire-format tests for the PDUs not covered by <see cref="PduRoundTripTests" />.</summary>
public class PduRoundTripRemainingTests
{
    private static string[] Body(PDUBase pdu, int prefixLength) =>
        PduTestHelpers.Fields(pdu.Serialize(), prefixLength);

    [Fact]
    public void AtcPosition_RoundTrips_WithMultipleFrequencies()
    {
        var pdu = new PDUATCPosition("egll_twr", [11895, 11840], NetworkFacility.Twr, 50, NetworkRating.C1, 51.47,
            -0.4543);
        Assert.StartsWith("%EGLL_TWR:11895&11840:", pdu.Serialize());
        var parsed = PDUATCPosition.Parse(Body(pdu, 1));
        Assert.Equal("EGLL_TWR", parsed.From);
        Assert.Equal([11895, 11840], parsed.Frequencies);
        Assert.Equal(NetworkFacility.Twr, parsed.Facility);
        Assert.Equal(50, parsed.VisibilityRange);
        Assert.Equal(NetworkRating.C1, parsed.Rating);
        Assert.Equal(51.47, parsed.Lat, 6);
        Assert.Equal(-0.4543, parsed.Lon, 6);
    }

    [Fact]
    public void AtcPosition_SingleFrequencyConstructor_ExposesFrequency()
    {
        var pdu = new PDUATCPosition("EGLL_TWR", 11895, NetworkFacility.Twr, 50, NetworkRating.C1, 0, 0);
        Assert.Equal(11895, pdu.Frequency);
        pdu.Frequency = 11840;
        Assert.Equal([11840], pdu.Frequencies);
    }

    [Fact]
    public void AtcPosition_NaNCoordinates_Throw()
    {
        Assert.ThrowsAny<Exception>(() =>
            new PDUATCPosition("EGLL_TWR", 11895, NetworkFacility.Twr, 50, NetworkRating.C1, double.NaN, 0));
    }

    [Fact]
    public void AddAtc_RoundTrips()
    {
        var pdu = new PDUAddATC("egll_twr", "Jane Doe", "1234567", "secret", NetworkRating.C1,
            ProtocolRevision.VatsimAuth);
        Assert.StartsWith("#AAEGLL_TWR:SERVER:Jane Doe:1234567:secret:", pdu.Serialize());
        var parsed = PDUAddATC.Parse(Body(pdu, 3));
        Assert.Equal("EGLL_TWR", parsed.From);
        Assert.Equal("Jane Doe", parsed.RealName);
        Assert.Equal("1234567", parsed.CID);
        Assert.Equal(NetworkRating.C1, parsed.Rating);

        // Parse deliberately drops the password and protocol revision; they are only ever sent, never read back.
        Assert.Equal("", parsed.Password);
        Assert.Equal(ProtocolRevision.Unknown, parsed.ProtocolRevision);
    }

    [Fact]
    public void ClientIdentification_RoundTrips()
    {
        var pdu = new PDUClientIdentification("abc123", 0xABCD, "xPilot", 3, 7, "1234567", "sysuid", "0123abcd");
        Assert.StartsWith("$IDABC123:SERVER:abcd:xPilot:3:7:1234567:sysuid:0123abcd", pdu.Serialize());
        var parsed = PDUClientIdentification.Parse(Body(pdu, 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal(0xABCD, parsed.ClientID);
        Assert.Equal("xPilot", parsed.ClientName);
        Assert.Equal(3, parsed.MajorVersion);
        Assert.Equal(7, parsed.MinorVersion);
        Assert.Equal("1234567", parsed.CID);
        Assert.Equal("sysuid", parsed.SysUID);
        Assert.Equal("0123abcd", parsed.InitialChallenge);
        Assert.Equal(parsed.InitialChallenge, parsed.InitialChallengeKey);
    }

    [Fact]
    public void ClientIdentification_InitialChallengeIsOptional()
    {
        var parsed = PDUClientIdentification.Parse(["ABC123", "SERVER", "ABCD", "xPilot", "3", "7", "1234567", "sysuid"]);
        Assert.Equal("", parsed.InitialChallenge);
    }

    [Fact]
    public void ClientQueryResponse_RoundTrips_WithPayload()
    {
        var pdu = new PDUClientQueryResponse("abc123", "def456", ClientQueryType.RealName, ["Jane Doe", "C1"]);
        Assert.StartsWith("$CRABC123:DEF456:", pdu.Serialize());
        var parsed = PDUClientQueryResponse.Parse(Body(pdu, 3));
        Assert.Equal(ClientQueryType.RealName, parsed.QueryType);
        Assert.Equal(["Jane Doe", "C1"], parsed.Payload);
    }

    [Fact]
    public void DeleteAtc_RoundTrips_AndCidIsOptional()
    {
        var pdu = new PDUDeleteATC("egll_twr", "1234567");
        Assert.Equal("#DAEGLL_TWR:1234567", pdu.Serialize());
        var parsed = PDUDeleteATC.Parse(Body(pdu, 3));
        Assert.Equal("EGLL_TWR", parsed.From);
        Assert.Equal("1234567", parsed.CID);
        Assert.Equal("", PDUDeleteATC.Parse(["EGLL_TWR"]).CID);
    }

    [Fact]
    public void FlightStrip_RoundTrips_WithoutAnnotations()
    {
        var pdu = new PDUFlightStrip("egll_twr", "egll_app", "baw123");
        var parsed = PDUFlightStrip.Parse(Body(pdu, 3));
        Assert.Equal("EGLL_TWR", parsed.From);
        Assert.Equal("EGLL_APP", parsed.To);
        Assert.Equal("BAW123", parsed.Target.ToUpperInvariant());
    }

    [Fact]
    public void FlightStrip_RoundTrips_WithAnnotations()
    {
        var pdu = new PDUFlightStrip("egll_twr", "egll_app", "baw123", "1", ["a", "b", "c"]);
        var parsed = PDUFlightStrip.Parse(Body(pdu, 3));
        Assert.Equal("1", parsed.FormatID);
        Assert.Equal(["a", "b", "c"], parsed.Annotations);
    }

    [Fact]
    public void KillRequest_RoundTrips_AndReasonIsOptional()
    {
        var pdu = new PDUKillRequest("sup1", "abc123", "Bad behavior");
        Assert.Equal("$!!SUP1:ABC123:Bad behavior", pdu.Serialize());
        var parsed = PDUKillRequest.Parse(Body(pdu, 3));
        Assert.Equal("SUP1", parsed.From);
        Assert.Equal("ABC123", parsed.To);
        Assert.Equal("Bad behavior", parsed.Reason);
        Assert.Equal("", PDUKillRequest.Parse(["SUP1", "ABC123"]).Reason);
    }

    [Theory]
    [InlineData(LandLineType.Intercom, LandLineCommand.Request, "IC")]
    [InlineData(LandLineType.Intercom, LandLineCommand.Approve, "IK")]
    [InlineData(LandLineType.Intercom, LandLineCommand.Reject, "IB")]
    [InlineData(LandLineType.Intercom, LandLineCommand.End, "EC")]
    [InlineData(LandLineType.Override, LandLineCommand.Request, "OV")]
    [InlineData(LandLineType.Override, LandLineCommand.Approve, "OK")]
    [InlineData(LandLineType.Override, LandLineCommand.Reject, "OB")]
    [InlineData(LandLineType.Override, LandLineCommand.End, "EO")]
    [InlineData(LandLineType.Monitor, LandLineCommand.Request, "MN")]
    [InlineData(LandLineType.Monitor, LandLineCommand.Approve, "MK")]
    [InlineData(LandLineType.Monitor, LandLineCommand.Reject, "MB")]
    [InlineData(LandLineType.Monitor, LandLineCommand.End, "EM")]
    public void LandLineCommand_RoundTrips_EveryTypeAndCommand(LandLineType type, LandLineCommand command,
        string code)
    {
        var pdu = new PDULandLineCommand("egll_twr", "egll_app", type, command, "10.0.0.1", 6000);
        Assert.Contains($":CCP:{code}", pdu.Serialize());
        var parsed = PDULandLineCommand.Parse(Body(pdu, 3));
        Assert.Equal(type, parsed.LandLineType);
        Assert.Equal(command, parsed.Command);

        // The endpoint is only sent with Request and Approve.
        var carriesEndpoint = command is LandLineCommand.Request or LandLineCommand.Approve;
        Assert.Equal(carriesEndpoint ? "10.0.0.1" : "", parsed.IP);
        Assert.Equal(carriesEndpoint ? 6000 : 0, parsed.Port);
    }

    [Fact]
    public void LandLineCommand_WithoutEndpoint_ParsesWithDefaults()
    {
        var parsed = PDULandLineCommand.Parse(Body(
            new PDULandLineCommand("egll_twr", "egll_app", LandLineType.Intercom, LandLineCommand.End), 3));
        Assert.Equal("", parsed.IP);
        Assert.Equal(0, parsed.Port);
    }

    [Fact]
    public void LandLineCommand_UnknownCode_ThrowsPduFormatException()
    {
        Assert.Throws<PDUFormatException>(() => PDULandLineCommand.Parse(["A", "B", "CCP", "XX"]));
    }

    [Fact]
    public void ModernClientCheck_RoundTrips()
    {
        var pdu = new PDUModernClientCheck("abc123", "def456");
        Assert.Equal("#PCABC123:DEF456:CCP:ID", pdu.Serialize());
        var parsed = PDUModernClientCheck.Parse(Body(pdu, 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("DEF456", parsed.To);
    }

    [Theory]
    [InlineData(true, "1")]
    [InlineData(false, "0")]
    public void Mute_RoundTrips(bool mute, string wire)
    {
        var pdu = new PDUMute("abc123", "def456", mute);
        Assert.EndsWith($":{wire}", pdu.Serialize());
        Assert.Equal(mute, PDUMute.Parse(Body(pdu, 3)).Mute);
    }

    [Theory]
    [InlineData(true, "1")]
    [InlineData(false, "0")]
    public void SendFastPositions_RoundTrips(bool send, string wire)
    {
        var pdu = new PDUSendFastPositions("server", "abc123", send);
        Assert.EndsWith($":{wire}", pdu.Serialize());
        Assert.Equal(send, PDUSendFastPositions.Parse(Body(pdu, 3)).Send);
    }

    [Theory]
    [InlineData(NetworkError.CallsignInUse, true)]
    [InlineData(NetworkError.InvalidLogon, true)]
    [InlineData(NetworkError.ServerFull, true)]
    [InlineData(NetworkError.SyntaxError, false)]
    [InlineData(NetworkError.NoSuchCallsign, false)]
    public void ProtocolError_RoundTrips_AndDerivesFatalFromErrorType(NetworkError type, bool fatal)
    {
        var pdu = new PDUProtocolError("server", "abc123", type, "param", "Something went wrong", fatal);
        var parsed = PDUProtocolError.Parse(Body(pdu, 3));
        Assert.Equal(type, parsed.ErrorType);
        Assert.Equal("param", parsed.Param);
        Assert.Equal("Something went wrong", parsed.Message);
        Assert.Equal(fatal, parsed.Fatal);
    }

    [Fact]
    public void PushToDepartureList_RoundTrips()
    {
        var pdu = new PDUPushToDepartureList("egll_twr", "egll_app", "baw123");
        Assert.Equal("#PCEGLL_TWR:EGLL_APP:CCP:DP:baw123", pdu.Serialize());
        Assert.Equal("baw123", PDUPushToDepartureList.Parse(Body(pdu, 3)).Target);
    }

    [Fact]
    public void Pointout_RoundTrips()
    {
        var pdu = new PDUPointout("egll_twr", "egll_app", "baw123");
        Assert.Equal("#PCEGLL_TWR:EGLL_APP:CCP:PT:baw123", pdu.Serialize());
        Assert.Equal("baw123", PDUPointout.Parse(Body(pdu, 3)).Target);
    }

    [Fact]
    public void IHaveTarget_RoundTrips()
    {
        var pdu = new PDUIHaveTarget("egll_twr", "egll_app", "baw123");
        Assert.Equal("#PCEGLL_TWR:EGLL_APP:CCP:IH:baw123", pdu.Serialize());
        Assert.Equal("baw123", PDUIHaveTarget.Parse(Body(pdu, 3)).Target);
    }

    [Fact]
    public void VersionRequest_RoundTrips()
    {
        var pdu = new PDUVersionRequest("egll_twr", "abc123");
        Assert.Equal("#PCEGLL_TWR:ABC123:CCP:VER", pdu.Serialize());
        var parsed = PDUVersionRequest.Parse(Body(pdu, 3));
        Assert.Equal("EGLL_TWR", parsed.From);
        Assert.Equal("ABC123", parsed.To);
    }

    [Fact]
    public void SecondaryVisCenter_RoundTrips()
    {
        var pdu = new PDUSecondaryVisCenter("egll_twr", 2, 51.47, -0.4543);
        var parsed = PDUSecondaryVisCenter.Parse(Body(pdu, 1));
        Assert.Equal("EGLL_TWR", parsed.From);
        Assert.Equal(2, parsed.Index);
        Assert.Equal(51.47, parsed.Lat, 6);
        Assert.Equal(-0.4543, parsed.Lon, 6);
    }

    [Fact]
    public void ServerIdentification_RoundTrips()
    {
        var pdu = new PDUServerIdentification("server", "client", "vatsim-fsd", "0123abcd");
        Assert.Equal("$DISERVER:CLIENT:vatsim-fsd:0123abcd", pdu.Serialize());
        var parsed = PDUServerIdentification.Parse(Body(pdu, 3));
        Assert.Equal("vatsim-fsd", parsed.Version);
        Assert.Equal("0123abcd", parsed.InitialChallengeKey);
    }

    [Fact]
    public void WeatherProfileRequest_RoundTrips()
    {
        var pdu = new PDUWeatherProfileRequest("abc123", "egll");
        var parsed = PDUWeatherProfileRequest.Parse(Body(pdu, 3));
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal("egll", parsed.Station);
    }

    [Fact]
    public void ChangeServer_RoundTrips()
    {
        var pdu = new PDUChangeServer("server", "abc123", "fsd2.example.net");
        Assert.Equal("$XXSERVER:ABC123:fsd2.example.net", pdu.Serialize());
        Assert.Equal("fsd2.example.net", PDUChangeServer.Parse(Body(pdu, 3)).NewServer);
    }

    [Fact]
    public void AuthResponse_RoundTrips()
    {
        var pdu = new PDUAuthResponse("abc123", "server", "deadbeef");
        Assert.Equal("$ZRABC123:SERVER:deadbeef", pdu.Serialize());
        Assert.Equal("deadbeef", PDUAuthResponse.Parse(Body(pdu, 3)).Response);
    }

    [Fact]
    public void Constructors_UppercaseFromAndTo()
    {
        var pdu = new PDUPing("abc123", "def456", "1");
        Assert.Equal("ABC123", pdu.From);
        Assert.Equal("DEF456", pdu.To);
    }

    [Fact]
    public void Reassemble_JoinsFieldsWithTheDelimiter() =>
        Assert.Equal("A:B:C", PDUBase.Reassemble(["A", "B", "C"]));
}
