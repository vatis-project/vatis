using System.Text;

namespace Vatsim.Network.PDU;

public class PDUSharedState : PDUBase
{
    public SharedStateType SharedStateType { get; set; }
    public string Target { get; set; }
    public string Value { get; set; }

    public PDUSharedState(string from, string to, SharedStateType sharedStateType, string target, string value)
        : base(from, to)
    {
        SharedStateType = sharedStateType;
        Target = target;
        Value = value;
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
        switch (SharedStateType)
        {
            case SharedStateType.Scratchpad:
                msg.Append("SC");
                break;
            case SharedStateType.BeaconCode:
                msg.Append("BC");
                break;
            case SharedStateType.VoiceType:
                msg.Append("VT");
                break;
            case SharedStateType.TempAlt:
                msg.Append("TA");
                break;
            case SharedStateType.GlobalData:
                msg.Append("GD");
                break;
        }
        msg.Append(DELIMITER);
        msg.Append(Target);
        msg.Append(DELIMITER);
        msg.Append(Value);
        return msg.ToString();
    }

    public static PDUSharedState Parse(string[] fields)
    {
        if (fields.Length < 6) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            SharedStateType sharedStateType = SharedStateType.Unknown;
            switch (fields[3])
            {
                case "SC":
                    sharedStateType = SharedStateType.Scratchpad;
                    break;
                case "BC":
                    sharedStateType = SharedStateType.BeaconCode;
                    break;
                case "VT":
                    sharedStateType = SharedStateType.VoiceType;
                    break;
                case "TA":
                    sharedStateType = SharedStateType.TempAlt;
                    break;
                case "GD":
                    sharedStateType = SharedStateType.GlobalData;
                    break;
            }
            return new PDUSharedState(
                fields[0],
                fields[1],
                sharedStateType,
                fields[4],
                fields[5]
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}