using System.Text;

namespace Vatsim.Network.PDU;

public class PDUPlaneInfoRequest : PDUBase
{
    public PDUPlaneInfoRequest(string from, string to)
        : base(from, to)
    {
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("#SB");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append("PIR");
        return msg.ToString();
    }

    public static PDUPlaneInfoRequest Parse(string[] fields)
    {
        if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUPlaneInfoRequest(
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