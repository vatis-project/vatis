namespace Vatsim.Network.Tests;

public class PitchBankHeadingTests
{
    private const double Step = 360.0 / 1024.0;

    [Fact]
    public void Pack_AllZero_IsZero() => Assert.Equal(0u, PbhAccessor.Pack(0, 0, 0));

    [Fact]
    public void Pack_PitchOccupiesTopTenBits() =>
        Assert.Equal(256u << 22, PbhAccessor.Pack(90, 0, 0));

    [Fact]
    public void Pack_NegativePitch_UsesTwosComplementStyleWrap() =>
        Assert.Equal(768u << 22, PbhAccessor.Pack(-90, 0, 0));

    [Fact]
    public void Pack_BankOccupiesBits12To21() =>
        Assert.Equal(256u << 12, PbhAccessor.Pack(0, 90, 0));

    [Fact]
    public void Pack_HeadingOccupiesBits2To11() =>
        Assert.Equal(256u << 2, PbhAccessor.Pack(0, 0, 90));

    [Theory]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(45.0, 90.0, 180.0)]
    [InlineData(-45.0, -90.0, 270.0)]
    [InlineData(-90.0, 90.0, 90.0)]
    [InlineData(180.0, 180.0, 0.0)]
    public void RoundTrip_ExactlyRepresentableValues_AreStable(double pitch, double bank, double heading)
    {
        var (p, b, h) = PbhAccessor.Unpack(PbhAccessor.Pack(pitch, bank, heading));
        Assert.Equal(pitch, p, 9);
        Assert.Equal(bank, b, 9);
        Assert.Equal(heading, h, 9);
    }

    [Fact]
    public void Unpack_NegativePitchAndBank_AreSigned()
    {
        var (p, b, _) = PbhAccessor.Unpack(PbhAccessor.Pack(-10.0, -20.0, 0));
        Assert.InRange(p, -10.0 - Step, -10.0 + Step);
        Assert.InRange(b, -20.0 - Step, -20.0 + Step);
        Assert.True(p < 0);
        Assert.True(b < 0);
    }

    [Fact]
    public void Heading_JustBelow360_TruncatesToLastStep()
    {
        var (_, _, h) = PbhAccessor.Unpack(PbhAccessor.Pack(0, 0, 359.9));
        Assert.Equal(1023 * Step, h, 9);
        Assert.True(h < 360.0);
    }

    [Fact]
    public void Heading_Zero_IsZero()
    {
        var (_, _, h) = PbhAccessor.Unpack(PbhAccessor.Pack(0, 0, 0));
        Assert.Equal(0.0, h);
    }

    [Fact]
    public void Heading_Exactly360_DoesNotCorruptBank()
    {
        var (_, b, h) = PbhAccessor.Unpack(PbhAccessor.Pack(0, 0, 360.0));
        Assert.Equal(0.0, b);
        Assert.Equal(0.0, h);
    }

    [Fact]
    public void Unpack_AllOnes_StaysWithinRanges()
    {
        var (p, b, h) = PbhAccessor.Unpack(uint.MaxValue);
        Assert.InRange(p, -180.0, 180.0);
        Assert.InRange(b, -180.0, 180.0);
        Assert.InRange(h, 0.0, 360.0);
        Assert.True(h < 360.0);
    }

    [Fact]
    public void Unpack_PitchAbove180_WrapsNegative()
    {
        var (p, _, _) = PbhAccessor.Unpack(768u << 22);
        Assert.Equal(-90.0, p, 9);
    }
}
