using System.Text;

namespace Vatsim.Network.PDU;

public class PDUProtocolError : PDUBase
{
    public NetworkError ErrorType { get; set; }
    public string Param { get; set; }
    public string Message { get; set; }
    public bool Fatal { get; set; }

    public PDUProtocolError(string from, string to, NetworkError type, string param, string msg, bool fatal)
        : base(from, to)
    {
        ErrorType = type;
        Param = param;
        Message = msg;
        Fatal = fatal;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("$ER");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append(((int)ErrorType).ToString("000"));
        msg.Append(DELIMITER);
        msg.Append(Param);
        msg.Append(DELIMITER);
        msg.Append(Message);
        return msg.ToString();
    }

    public static PDUProtocolError Parse(string[] fields)
    {
        if (fields.Length < 5) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            NetworkError err = (NetworkError)int.Parse(fields[2]);
            bool fatal = err == NetworkError.CallsignInUse || err == NetworkError.CallsignInvalid || err == NetworkError.AlreadyRegistered || err == NetworkError.InvalidLogon || err == NetworkError.InvalidProtocolRevision || err == NetworkError.RequestedLevelTooHigh || err == NetworkError.ServerFull || err == NetworkError.CertificateSuspended || err == NetworkError.InvalidPositionForRating || err == NetworkError.UnauthorizedSoftware || err == NetworkError.AuthenticationResponseTimeout || err == NetworkError.InvalidClientVersion;
            return new PDUProtocolError(
                fields[0],
                fields[1],
                err,
                fields[3],
                fields[4],
                fatal
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}