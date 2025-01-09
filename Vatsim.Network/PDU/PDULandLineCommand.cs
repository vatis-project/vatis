using System.Text;

namespace Vatsim.Network.PDU;

public class PDULandLineCommand : PDUBase
{
    private static readonly string[,] sCommandMap = new string[3, 4] {
        { "IC", "IK", "IB", "EC" },
        { "OV", "OK", "OB", "EO" },
        { "MN", "MK", "MB", "EM" }
    };

    public LandLineType LandLineType { get; set; }
    public LandLineCommand Command { get; set; }
    public string IP { get; set; }
    public int Port { get; set; }

    public PDULandLineCommand(string from, string to, LandLineType landLineType, LandLineCommand cmd, string ip, int port)
        : base(from, to)
    {
        LandLineType = landLineType;
        Command = cmd;
        IP = ip;
        Port = port;
    }

    public PDULandLineCommand(string from, string to, LandLineType landLineType, LandLineCommand cmd)
        : this(from, to, landLineType, cmd, "", 0)
    {
    }

    private static bool LookupCommand(string identifier, out LandLineType landLineType, out LandLineCommand cmd)
    {
        bool found = false;
        landLineType = LandLineType.Override;
        cmd = LandLineCommand.Request;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (sCommandMap[i, j] == identifier)
                {
                    landLineType = (LandLineType)i;
                    cmd = (LandLineCommand)j;
                    return true;
                }
            }
        }
        return found;
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
        msg.Append(sCommandMap[(int)LandLineType, (int)Command]);
        if (Command == LandLineCommand.Request || Command == LandLineCommand.Approve)
        {
            msg.Append(DELIMITER);
            msg.Append(IP);
            msg.Append(DELIMITER);
            msg.Append(Port);
        }
        return msg.ToString();
    }

    public static PDULandLineCommand Parse(string[] fields)
    {
        if (fields.Length < 4) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            LandLineType landLineType;
            LandLineCommand cmd;
            if (!LookupCommand(fields[3], out landLineType, out cmd)) throw new PDUFormatException("Unknown land line command type: {0}", Reassemble(fields));
            return new PDULandLineCommand(
                fields[0],
                fields[1],
                landLineType,
                cmd,
                fields.Length >= 6 ? fields[4] : "",
                fields.Length >= 6 ? int.Parse(fields[5]) : 0
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}