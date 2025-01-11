using System.Text;

namespace Vatsim.Network.PDU;

public class PDUSendFastPositions : PDUBase
{
    public bool Send { get; set; }

    public PDUSendFastPositions(string from, string to, bool send)
        : base(from, to)
    {
        Send = send;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("$SF");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append(Send ? "1" : "0");
        return msg.ToString();
    }

    public static PDUSendFastPositions Parse(string[] fields)
    {
        if (fields.Length < 3)
        {
            throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        }

        try
        {
            return new PDUSendFastPositions(
                fields[0],
                fields[1],
                fields[2] == "1"
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}