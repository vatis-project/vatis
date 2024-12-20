using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUHandoff : PDUBase
    {
        public string Target { get; set; }

        public PDUHandoff(string from, string to, string target)
            : base(from, to)
        {
            Target = target;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("$HO");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(Target);
            return msg.ToString();
        }

        public static PDUHandoff Parse(string[] fields)
        {
            if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUHandoff(
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
