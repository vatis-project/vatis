using System.Text;

namespace Vatsim.Network.PDU;

public class PDUDeleteATC : PDUBase
{
    public string CID { get; set; }

    public PDUDeleteATC(string from, string cid)
        : base(from, "")
    {
        CID = cid;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("#DA");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(CID);
        return msg.ToString();
    }

    public static PDUDeleteATC Parse(string[] fields)
    {
        if (fields.Length < 1) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUDeleteATC(
                fields[0],
                fields.Length >= 2 ? fields[1] : ""
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}