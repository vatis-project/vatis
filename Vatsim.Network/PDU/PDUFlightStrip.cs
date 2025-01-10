using System.Text;

namespace Vatsim.Network.PDU;

public class PDUFlightStrip : PDUBase
{
    public string Target { get; set; }
    public string FormatID { get; set; }
    public List<string> Annotations { get; set; }

    public PDUFlightStrip(string from, string to, string target)
        : base(from, to)
    {
        Target = target;
        FormatID = "";
        Annotations = new List<string>();
    }

    public PDUFlightStrip(string from, string to, string target, string formatID, List<string> annotations)
        : base(from, to)
    {
        Target = target;
        FormatID = formatID;
        Annotations = annotations;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("#PC");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append("CCP");
        msg.Append(DELIMITER);
        msg.Append("ST");
        msg.Append(DELIMITER);
        msg.Append(Target);
        if (!string.IsNullOrEmpty(FormatID) || Annotations.Count > 0)
        {
            msg.Append(DELIMITER);
            msg.Append(FormatID);
            foreach (string annotation in Annotations)
            {
                msg.Append(DELIMITER);
                msg.Append(annotation);
            }
        }
        return msg.ToString();
    }

    public static PDUFlightStrip Parse(string[] fields)
    {
        if (fields.Length < 5) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            if (fields.Length > 5)
            {
                List<string> annotations = new List<string>();
                if (fields.Length > 6)
                {
                    for (int i = 6; i < fields.Length; i++)
                    {
                        annotations.Add(fields[i]);
                    }
                }
                return new PDUFlightStrip(
                    fields[0],
                    fields[1],
                    fields[4],
                    fields[5],
                    annotations
                );
            }
            else
            {
                return new PDUFlightStrip(
                    fields[0],
                    fields[1],
                    fields[4]
                );
            }
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}