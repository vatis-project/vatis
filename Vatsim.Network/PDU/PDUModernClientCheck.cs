using System.Text;

namespace Vatsim.Network.PDU;

public class PDUModernClientCheck : PDUBase
{
    public PDUModernClientCheck(string from, string to)
        : base(from, to)
    {
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("#PC");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append("CCP");
        msg.Append(DELIMITER);
        msg.Append("ID");
        return msg.ToString();
    }

    public static PDUModernClientCheck Parse(string[] fields)
    {
        if (fields.Length < 4) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUModernClientCheck(
                fields[0],
                fields[1]
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}