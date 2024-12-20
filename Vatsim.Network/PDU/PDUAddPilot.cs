using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUAddPilot : PDUBase
    {
        public string CID { get; set; }
        public string Password { get; set; }
        public NetworkRating Rating { get; set; }
        public ProtocolRevision ProtocolRevision { get; set; }
        public SimulatorType SimulatorType { get; set; }
        public string RealName { get; set; }

        public PDUAddPilot(string callsign, string cid, string password, NetworkRating rating, ProtocolRevision proto, SimulatorType simType, string realName)
            : base(callsign, "")
        {
            CID = cid;
            Password = password;
            Rating = rating;
            ProtocolRevision = proto;
            SimulatorType = simType;
            RealName = realName;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("#AP");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(SERVER_CALLSIGN);
            msg.Append(DELIMITER);
            msg.Append(CID);
            msg.Append(DELIMITER);
            msg.Append(Password);
            msg.Append(DELIMITER);
            msg.Append((int)Rating);
            msg.Append(DELIMITER);
            msg.Append((int)ProtocolRevision);
            msg.Append(DELIMITER);
            msg.Append((int)SimulatorType);
            msg.Append(DELIMITER);
            msg.Append(RealName);
            return msg.ToString();
        }

        public static PDUAddPilot Parse(string[] fields)
        {
            if (fields.Length < 8) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUAddPilot(
                    fields[0],
                    fields[2],
                    fields[3],
                    (NetworkRating)int.Parse(fields[4]),
                    (ProtocolRevision)int.Parse(fields[5]),
                    (SimulatorType)int.Parse(fields[6]),
                    fields[7]
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
