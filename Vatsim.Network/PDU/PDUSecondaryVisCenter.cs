using System.Globalization;
using System.Text;

namespace Vatsim.Network.PDU;

public class PDUSecondaryVisCenter : PDUBase
{
    public int Index { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }

    public PDUSecondaryVisCenter(string from, int index, double lat, double lon)
        : base(from, "")
    {
        Index = index;
        Lat = lat;
        Lon = lon;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("'");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(Index);
        msg.Append(DELIMITER);
        msg.Append(Lat.ToString("#0.00000", CultureInfo.InvariantCulture));
        msg.Append(DELIMITER);
        msg.Append(Lon.ToString("#0.00000", CultureInfo.InvariantCulture));
        return msg.ToString();
    }

    public static PDUSecondaryVisCenter Parse(string[] fields)
    {
        if (fields.Length < 4) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUSecondaryVisCenter(
                fields[0],
                int.Parse(fields[1]),
                double.Parse(fields[2], CultureInfo.InvariantCulture),
                double.Parse(fields[3], CultureInfo.InvariantCulture)
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}