using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUWallop : PDUBase
    {
        public string Message { get; set; }

        public PDUWallop(string from, string message)
            : base(from, "*S")
        {
            Message = message;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("#TM");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(Message);
            return msg.ToString();
        }

        public static PDUWallop Parse(string[] fields)
        {
            if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            StringBuilder msg = new StringBuilder(fields[2]);
            for (int i = 3; i < fields.Length; i++) msg.AppendFormat(":{0}", fields[i]);
            try
            {
                return new PDUWallop(
                    fields[0],
                    msg.ToString()
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
