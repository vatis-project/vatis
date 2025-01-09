using System.Text;

namespace Vatsim.Network.PDU;

public class PDUAddATC : PDUBase
{
    public string RealName { get; set; }
    public string CID { get; set; }
    public string Password { get; set; }
    public NetworkRating Rating { get; set; }
    public ProtocolRevision ProtocolRevision { get; set; }

    public PDUAddATC(string callsign, string realName, string cid, string password, NetworkRating rating, ProtocolRevision proto)
        : base(callsign, "")
    {
        RealName = realName;
        CID = cid;
        Password = password;
        Rating = rating;
        ProtocolRevision = proto;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("#AA");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(SERVER_CALLSIGN);
        msg.Append(DELIMITER);
        msg.Append(RealName);
        msg.Append(DELIMITER);
        msg.Append(CID);
        msg.Append(DELIMITER);
        msg.Append(Password);
        msg.Append(DELIMITER);
        msg.Append((int)Rating);
        msg.Append(DELIMITER);
        msg.Append((int)ProtocolRevision);
        return msg.ToString();
    }

    public static PDUAddATC Parse(string[] fields)
    {
        if (fields.Length < 6) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUAddATC(
                fields[0],
                fields[2],
                fields[3],
                "",
                (NetworkRating)int.Parse(fields[5]),
                ProtocolRevision.Unknown
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}