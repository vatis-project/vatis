using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUMetarResponse : PDUBase
    {
        public string Metar { get; set; }

        public PDUMetarResponse(string to, string metar)
            : base(SERVER_CALLSIGN, to)
        {
            Metar = metar;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("$AR");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(Metar);
            return msg.ToString();
        }

        public static PDUMetarResponse Parse(string[] fields)
        {
            if (fields.Length < 4) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUMetarResponse(
                    fields[1],
                    fields[3]
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
