using Vatsim.Network.PDU;

namespace Vatsim.Network.Tests;

internal static class PduTestHelpers
{
    public static string[] Fields(string serialized, int prefixLength) =>
        serialized[prefixLength..].Split(PDUBase.DELIMITER);
}

internal sealed class PbhAccessor : PDUBase
{
    public PbhAccessor() : base("A", "B")
    {
    }

    public override string Serialize() => string.Empty;

    public static uint Pack(double pitch, double bank, double heading) => PackPitchBankHeading(pitch, bank, heading);

    public static (double Pitch, double Bank, double Heading) Unpack(uint pbh)
    {
        UnpackPitchBankHeading(pbh, out var pitch, out var bank, out var heading);
        return (pitch, bank, heading);
    }
}
