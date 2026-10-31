using Vatsim.Network.PDU;

namespace Vatsim.Network.Tests;

public class FastPilotPositionTests
{
    private static PDUFastPilotPosition Make(FastPilotPositionType type) =>
        new(type, "abc123", 47.4489, -122.3094, 1500.5, 20.25, 5.0, 90.0, -10.0,
            1.5, -2.25, 3.125, 0.5, -0.5, 0.25, 12.5);

    [Theory]
    [InlineData(FastPilotPositionType.Fast, 12, "^")]
    [InlineData(FastPilotPositionType.Slow, 12, "#SL")]
    [InlineData(FastPilotPositionType.Stopped, 6, "#ST")]
    public void FieldCountAndPrefix(FastPilotPositionType type, int fieldCount, string prefix)
    {
        Assert.Equal(fieldCount, type.FieldCount());
        Assert.Equal(prefix, type.Prefix());
    }

    [Fact]
    public void UnknownType_Throws()
    {
        Assert.Throws<ApplicationException>(() => ((FastPilotPositionType)99).FieldCount());
        Assert.Throws<ApplicationException>(() => ((FastPilotPositionType)99).Prefix());
    }

    [Theory]
    [InlineData(FastPilotPositionType.Fast)]
    [InlineData(FastPilotPositionType.Slow)]
    public void MovingVariants_RoundTripAllFields(FastPilotPositionType type)
    {
        var serialized = Make(type).Serialize();
        var prefix = type.Prefix();
        Assert.StartsWith(prefix + "ABC123:47.4489000:-122.3094000:1500.50:20.25:", serialized);

        var fields = PduTestHelpers.Fields(serialized, prefix.Length);
        Assert.Equal(13, fields.Length);

        var parsed = PDUFastPilotPosition.Parse(type, fields);
        Assert.Equal(type, parsed.Type);
        Assert.Equal("ABC123", parsed.From);
        Assert.Equal(47.4489, parsed.Lat, 6);
        Assert.Equal(-122.3094, parsed.Lon, 6);
        Assert.Equal(1500.5, parsed.AltitudeTrue);
        Assert.Equal(20.25, parsed.AltitudeAgl);
        Assert.InRange(parsed.Pitch, 5.0 - 0.36, 5.0 + 0.36);
        Assert.InRange(parsed.Heading, 90.0 - 0.36, 90.0 + 0.36);
        Assert.InRange(parsed.Bank, -10.0 - 0.36, -10.0 + 0.36);
        Assert.Equal(1.5, parsed.VelocityLongitude);
        Assert.Equal(-2.25, parsed.VelocityAltitude);
        Assert.Equal(3.125, parsed.VelocityLatitude);
        Assert.Equal(0.5, parsed.VelocityPitch);
        Assert.Equal(-0.5, parsed.VelocityHeading);
        Assert.Equal(0.25, parsed.VelocityBank);
        Assert.Equal(12.5, parsed.NoseGearAngle);
    }

    [Fact]
    public void Stopped_OmitsVelocities_AndRoundTrips()
    {
        var serialized = Make(FastPilotPositionType.Stopped).Serialize();
        var fields = PduTestHelpers.Fields(serialized, 3);
        Assert.StartsWith("#STABC123:", serialized);
        Assert.Equal(7, fields.Length);

        var parsed = PDUFastPilotPosition.Parse(FastPilotPositionType.Stopped, fields);
        Assert.Equal(12.5, parsed.NoseGearAngle);
        Assert.Equal(20.25, parsed.AltitudeAgl);
        Assert.Equal(0.0, parsed.VelocityLongitude);
        Assert.Equal(0.0, parsed.VelocityBank);
    }

    [Fact]
    public void NoseGearAngle_IsOptional()
    {
        var moving = PDUFastPilotPosition.Parse(FastPilotPositionType.Fast,
            ["ABC", "1", "2", "3", "4", "0", "1", "2", "3", "4", "5", "6"]);
        Assert.Equal(0.0, moving.NoseGearAngle);
        Assert.Equal(6.0, moving.VelocityBank);

        var stopped = PDUFastPilotPosition.Parse(FastPilotPositionType.Stopped, ["ABC", "1", "2", "3", "4", "0"]);
        Assert.Equal(0.0, stopped.NoseGearAngle);
    }

    [Theory]
    [InlineData(FastPilotPositionType.Fast)]
    [InlineData(FastPilotPositionType.Slow)]
    [InlineData(FastPilotPositionType.Stopped)]
    public void Parse_TooFewFields_Throws(FastPilotPositionType type)
    {
        var fields = Enumerable.Repeat("0", type.FieldCount() - 1).ToArray();
        var ex = Assert.Throws<PDUFormatException>(() => PDUFastPilotPosition.Parse(type, fields));
        Assert.Equal("Invalid field count.", ex.Message);
    }

    [Fact]
    public void Parse_NonNumericField_ThrowsFormatExceptionWithInner()
    {
        var ex = Assert.Throws<PDUFormatException>(() =>
            PDUFastPilotPosition.Parse(FastPilotPositionType.Stopped, ["ABC", "lat", "2", "3", "4", "0"]));
        Assert.Equal("Parse error.", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void Constructor_NaNLatOrLon_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PDUFastPilotPosition(FastPilotPositionType.Fast, "A", double.NaN,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        Assert.Throws<ArgumentException>(() => new PDUFastPilotPosition(FastPilotPositionType.Fast, "A", 0,
            double.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    }
}
