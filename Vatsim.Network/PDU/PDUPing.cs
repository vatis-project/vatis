using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUPing : PDUBase
    {
        public string TimeStamp { get; set; }

        public PDUPing(string from, string to, string timeStamp)
            : base(from, to)
        {
            TimeStamp = timeStamp;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("$PI");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(TimeStamp);
            return msg.ToString();
        }

        public static PDUPing Parse(string[] fields)
        {
            if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUPing(
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
