using System.Text;

namespace Vatsim.Network.PDU;

public class PDUWeatherProfileRequest : PDUBase
{
    public string Station { get; set; }

    public PDUWeatherProfileRequest(string from, string station)
        : base(from, SERVER_CALLSIGN)
    {
        Station = station;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("#WX");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append(Station);
        return msg.ToString();
    }

    public static PDUWeatherProfileRequest Parse(string[] fields)
    {
        if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUWeatherProfileRequest(
                fields[0],
                fields[2]
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}