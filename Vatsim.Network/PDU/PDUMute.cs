using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUMute : PDUBase
    {
        public bool Mute { get; set; }

        public PDUMute(string from, string to, bool mute)
            : base(from, to)
        {
            Mute = mute;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("#MU");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(Mute ? "1" : "0");
            return msg.ToString();
        }

        public static PDUMute Parse(string[] fields)
        {
            if (fields.Length < 3)
            {
                throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            }

            try
            {
                return new PDUMute(
                    fields[0],
                    fields[1],
                    fields[2] == "1"
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
