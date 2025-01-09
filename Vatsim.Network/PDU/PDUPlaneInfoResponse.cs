using System.Text;

namespace Vatsim.Network.PDU;

public class PDUPlaneInfoResponse : PDUBase
{
    public string Equipment { get; set; }
    public string Airline { get; set; }
    public string Livery { get; set; }
    public string CSL { get; set; }

    public PDUPlaneInfoResponse(string from, string to, string equipment, string airline, string livery, string csl)
        : base(from, to)
    {
        Equipment = equipment;
        Airline = airline;
        Livery = livery;
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
        msg.Append("GEN");
        msg.Append(DELIMITER);
        msg.AppendFormat("EQUIPMENT={0}", Equipment);
        if (!string.IsNullOrEmpty(Airline))
        {
            msg.Append(DELIMITER);
            msg.AppendFormat("AIRLINE={0}", Airline);
        }
        if (!string.IsNullOrEmpty(Livery))
        {
            msg.Append(DELIMITER);
            msg.AppendFormat("LIVERY={0}", Livery);
        }
        if (!string.IsNullOrEmpty(CSL))
        {
            msg.Append(DELIMITER);
            msg.AppendFormat("CSL={0}", CSL);
        }
        return msg.ToString();
    }

    public static PDUPlaneInfoResponse Parse(string[] fields)
    {
        if (fields.Length < 5) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUPlaneInfoResponse(
                fields[0],
                fields[1],
                FindValue(fields, "EQUIPMENT"),
                FindValue(fields, "AIRLINE"),
                FindValue(fields, "LIVERY"),
                FindValue(fields, "CSL")
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }

    private static string FindValue(string[] fields, string key)
    {
        foreach (string field in fields)
        {
            if (field.ToUpper().StartsWith(key.ToUpper() + "=")) return field.Substring(key.Length + 1);
        }
        return "";
    }
}