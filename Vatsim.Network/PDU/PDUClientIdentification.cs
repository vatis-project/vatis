using System.Text;

namespace Vatsim.Network.PDU;

public class PDUClientIdentification : PDUBase
{
    public ushort ClientID { get; set; }
    public string ClientName { get; set; }
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public string CID { get; set; }
    public string SysUID { get; set; }
    public string InitialChallenge { get; set; }
    [Obsolete("Use InitialChallenge instead. InitialChallengeKey is a misnomer.")]
    public string InitialChallengeKey
    {
        get { return InitialChallenge; }
        set { InitialChallenge = value; }
    }

    public PDUClientIdentification(string from, ushort clientID, string clientName, int majorVersion, int minorVersion, string cid, string sysUID, string initialChallenge)
        : base(from, SERVER_CALLSIGN)
    {
        ClientID = clientID;
        ClientName = clientName;
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        CID = cid;
        SysUID = sysUID;
        InitialChallenge = initialChallenge;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("$ID");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append(ClientID.ToString("x4"));
        msg.Append(DELIMITER);
        msg.Append(ClientName);
        msg.Append(DELIMITER);
        msg.Append(MajorVersion.ToString());
        msg.Append(DELIMITER);
        msg.Append(MinorVersion.ToString());
        msg.Append(DELIMITER);
        msg.Append(CID);
        msg.Append(DELIMITER);
        msg.Append(SysUID);
        if (!string.IsNullOrEmpty(InitialChallenge))
        {
            msg.Append(DELIMITER);
            msg.Append(InitialChallenge);
        }
        return msg.ToString();
    }

    public static PDUClientIdentification Parse(string[] fields)
    {
        if (fields.Length < 8) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUClientIdentification(
                fields[0],
                Convert.ToUInt16(fields[2], 16),
                fields[3],
                Convert.ToInt32(fields[4]),
                Convert.ToInt32(fields[5]),
                fields[6],
                fields[7],
                fields.Length > 8 ? fields[8] : ""
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}