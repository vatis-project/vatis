using System.Text;

namespace Vatsim.Network.PDU;

public class PDUServerIdentification : PDUBase
{
    public string Version { get; set; }
    public string InitialChallengeKey { get; set; }

    public PDUServerIdentification(string from, string to, string version, string initialChallengeKey)
        : base(from, to)
    {
        Version = version;
        InitialChallengeKey = initialChallengeKey;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("$DI");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append(Version);
        msg.Append(DELIMITER);
        msg.Append(InitialChallengeKey);
        return msg.ToString();
    }

    public static PDUServerIdentification Parse(string[] fields)
    {
        if (fields.Length < 4) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUServerIdentification(
                fields[0],
                fields[1],
                fields[2],
                fields[3]
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}