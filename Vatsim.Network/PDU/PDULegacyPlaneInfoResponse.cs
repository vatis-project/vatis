using System.Text;

namespace Vatsim.Network.PDU;

public class PDULegacyPlaneInfoResponse : PDUBase
{
    public EngineType EngineType { get; set; }
    public string CSL { get; set; }

    public PDULegacyPlaneInfoResponse(string from, string to, EngineType engineType, string csl)
        : base(from, to)
    {
        EngineType = engineType;
        CSL = csl;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("#SB");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append("PI");
        msg.Append(DELIMITER);
        msg.Append("X");
        msg.Append(DELIMITER);
        msg.Append("0");
        msg.Append(DELIMITER);
        msg.Append((int)EngineType);
        msg.Append(DELIMITER);
        msg.AppendFormat("CSL={0}", CSL);
        return msg.ToString();
    }

    public static PDULegacyPlaneInfoResponse Parse(string[] fields)
    {
        if (fields.Length < 6) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDULegacyPlaneInfoResponse(
                fields[0],
                fields[1],
                (EngineType)Enum.Parse(typeof(EngineType), fields[5]),
                fields[6]
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}