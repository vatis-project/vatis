using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUMetarRequest : PDUBase
    {
        public string Station { get; set; }

        public PDUMetarRequest(string from, string station)
            : base(from, SERVER_CALLSIGN)
        {
            Station = station;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("$AX");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append("METAR");
            msg.Append(DELIMITER);
            msg.Append(Station);
            return msg.ToString();
        }

        public static PDUMetarRequest Parse(string[] fields)
        {
            if (fields.Length < 4) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUMetarRequest(
                    fields[0],
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
