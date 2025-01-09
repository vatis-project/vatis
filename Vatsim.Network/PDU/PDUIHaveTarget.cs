﻿using System.Text;

namespace Vatsim.Network.PDU;

public class PDUIHaveTarget : PDUBase
{
    public string Target { get; set; }

    public PDUIHaveTarget(string from, string to, string target)
        : base(from, to)
    {
        Target = target;
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
        msg.Append("IH");
        msg.Append(DELIMITER);
        msg.Append(Target);
        return msg.ToString();
    }

    public static PDUIHaveTarget Parse(string[] fields)
    {
        if (fields.Length < 5) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            return new PDUIHaveTarget(
                fields[0],
                fields[1],
                fields[4]
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}