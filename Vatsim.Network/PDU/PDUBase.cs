namespace Vatsim.Network.PDU;

public abstract class PDUBase
{
    public const string CLIENT_QUERY_BROADCAST_RECIPIENT = "@94835";
    public const string CLIENT_QUERY_BROADCAST_RECIPIENT_PILOTS = "@94836";
    public const char DELIMITER = ':';
    public const string PACKET_DELIMITER = "\r\n";
    public const string SERVER_CALLSIGN = "SERVER";

    public string From { get; set; }
    public string To { get; set; }

    public PDUBase(string from, string to)
    {
        From = from;
        To = to;
    }

    public abstract string Serialize();

    public static string Reassemble(string[] fields)
    {
        return string.Join(DELIMITER.ToString(), fields);
    }

    protected static uint PackPitchBankHeading(double pitch, double bank, double heading)
    {
        double p = pitch / 360.0;
        if (p < 0)
        {
            p += 1.0;
        }
        p *= 1024.0;

        double b = bank / 360.0;
        if (b < 0)
        {
            b += 1.0;
        }
        b *= 1024.0;

        double h = heading / 360.0 * 1024.0;

        return (uint)p << 22 | (uint)b << 12 | (uint)h << 2;
    }

    protected static void UnpackPitchBankHeading(uint pbh, out double pitch, out double bank, out double heading)
    {
        uint pitchInt = pbh >> 22;
        pitch = pitchInt / 1024.0 * 360.0;
        if (pitch > 180.0)
        {
            pitch -= 360.0;
        }
        else if (pitch <= -180.0)
        {
            pitch += 360.0;
        }

        uint bankInt = pbh >> 12 & 0x3FF;
        bank = bankInt / 1024.0 * 360.0;
        if (bank > 180.0)
        {
            bank -= 360.0;
        }
        else if (bank <= -180.0)
        {
            bank += 360.0;
        }

        uint hdgInt = pbh >> 2 & 0x3FF;
        heading = hdgInt / 1024.0 * 360.0;
        if (heading < 0.0)
        {
            heading += 360.0;
        }
        else if (heading >= 360.0)
        {
            heading -= 360.0;
        }
    }
}