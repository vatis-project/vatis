using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUAuthResponse : PDUBase
    {
        public string Response { get; set; }

        public PDUAuthResponse(string from, string to, string response)
            : base(from, to)
        {
            Response = response;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("$ZR");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(Response);
            return msg.ToString();
        }

        public static PDUAuthResponse Parse(string[] fields)
        {
            if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUAuthResponse(
                    fields[0],
                    fields[1],
                    fields[2]
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
