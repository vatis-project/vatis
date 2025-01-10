using System.Text;

namespace Vatsim.Network.PDU;

public class PDUAuthChallenge : PDUBase
{
    public string Challenge { get; set; }

    public PDUAuthChallenge(string from, string to, string challenge)
        : base(from, to)
    {
        Challenge = challenge;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("$ZC");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append(Challenge);
        return msg.ToString();
    }

    public static PDUAuthChallenge Parse(string[] fields)
    {
        if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUAuthChallenge(
                fields[0],
                fields[1],
                fields[2]
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}