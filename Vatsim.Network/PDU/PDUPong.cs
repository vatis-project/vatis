using System.Text;

namespace Vatsim.Network.PDU;

public class PDUPong : PDUBase
{
    public string TimeStamp { get; set; }

    public PDUPong(string from, string to, string timeStamp)
        : base(from, to)
    {
        TimeStamp = timeStamp;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("$PO");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append(TimeStamp);
        return msg.ToString();
    }

    public static PDUPong Parse(string[] fields)
    {
        if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUPong(
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