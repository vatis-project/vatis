using System.Globalization;
using System.Text;

namespace Vatsim.Network.PDU;

public class PDUPilotPosition : PDUBase
{
    private bool mIsSquawkingModeC;

    public int SquawkCode { get; set; }
    public bool IsSquawkingModeC { get { return mIsSquawkingModeC; } set { mIsSquawkingModeC = value; } }
    public bool IsSquawkingCharlie { get { return mIsSquawkingModeC; } set { mIsSquawkingModeC = value; } }
    public bool IsTransponderOn { get { return mIsSquawkingModeC; } set { mIsSquawkingModeC = value; } }
    public bool IsIdenting { get; set; }
    public NetworkRating Rating { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public int TrueAltitude { get; set; }
    public int PressureAltitude { get; set; }
    public int GroundSpeed { get; set; }
    public double Pitch { get; set; }
    public double Heading { get; set; }
    public double Bank { get; set; }

    public PDUPilotPosition(string from, int txCode, bool squawkingModeC, bool identing, NetworkRating rating, double lat, double lon, int trueAlt, int pressureAlt, int gs, double pitch, double heading, double bank)
        : base(from, "")
    {
        if (double.IsNaN(lat))
        {
            throw new ArgumentException("Latitude must be a valid double precision number.", "lat");
        }

        if (double.IsNaN(lon))
        {
            throw new ArgumentException("Longitude must be a valid double precision number.", "lon");
        }

        SquawkCode = txCode;
        mIsSquawkingModeC = squawkingModeC;
        IsIdenting = identing;
        Rating = rating;
        Lat = lat;
        Lon = lon;
        TrueAltitude = trueAlt;
        PressureAltitude = pressureAlt;
        GroundSpeed = gs;
        Pitch = pitch;
        Heading = heading;
        Bank = bank;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("@");
        msg.Append(IsIdenting ? "Y" : mIsSquawkingModeC ? "N" : "S");
        msg.Append(DELIMITER);
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(SquawkCode.ToString("0000"));
        msg.Append(DELIMITER);
        msg.Append((int)Rating);
        msg.Append(DELIMITER);
        msg.Append(Lat.ToString("#0.0000000", CultureInfo.InvariantCulture));
        msg.Append(DELIMITER);
        msg.Append(Lon.ToString("#0.0000000", CultureInfo.InvariantCulture));
        msg.Append(DELIMITER);
        msg.Append(TrueAltitude);
        msg.Append(DELIMITER);
        msg.Append(GroundSpeed);
        msg.Append(DELIMITER);
        msg.Append(PackPitchBankHeading(Pitch, Bank, Heading));
        msg.Append(DELIMITER);
        msg.Append(PressureAltitude - TrueAltitude);
        return msg.ToString();
    }

    public static PDUPilotPosition Parse(string[] fields)
    {
        if (fields.Length < 10)
        {
            throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        }

        try
        {
            bool identing = false;
            bool charlie = false;
            switch (fields[0].ToUpper())
            {
                case "S":
                    break;
                case "N":
                    charlie = true;
                    break;
                case "Y":
                    charlie = true;
                    identing = true;
                    break;
            }
            UnpackPitchBankHeading(uint.Parse(fields[8]), out double pitch, out double bank, out double heading);
            return new PDUPilotPosition(
                fields[1],
                int.Parse(fields[2]),
                charlie,
                identing,
                (NetworkRating)Enum.Parse(typeof(NetworkRating), fields[3]),
                double.Parse(fields[4], CultureInfo.InvariantCulture),
                double.Parse(fields[5], CultureInfo.InvariantCulture),
                Convert.ToInt32(double.Parse(fields[6], CultureInfo.InvariantCulture)),
                Convert.ToInt32(double.Parse(fields[6], CultureInfo.InvariantCulture) + double.Parse(fields[9], CultureInfo.InvariantCulture)),
                Convert.ToInt32(double.Parse(fields[7], CultureInfo.InvariantCulture)),
                pitch,
                heading,
                bank
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}