using System.Text;

namespace Vatsim.Network.PDU;

public class PDUKillRequest : PDUBase
{
    public string Reason { get; set; }

    public PDUKillRequest(string from, string victim, string reason)
        : base(from, victim)
    {
        Reason = reason;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("$!!");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append(Reason);
        return msg.ToString();
    }

    public static PDUKillRequest Parse(string[] fields)
    {
        if (fields.Length < 2) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUKillRequest(
                fields[0],
                fields[1],
                fields.Length > 2 ? fields[2] : ""
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}