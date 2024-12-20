using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUVersionRequest : PDUBase
    {
        public PDUVersionRequest(string from, string to)
            : base(from, to)
        {
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
            msg.Append("VER");
            return msg.ToString();
        }

        public static PDUVersionRequest Parse(string[] fields)
        {
            if (fields.Length < 4) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUVersionRequest(
                    fields[0],
                    fields[1]
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
