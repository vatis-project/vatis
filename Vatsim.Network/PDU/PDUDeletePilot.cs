using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUDeletePilot : PDUBase
    {
        public string CID { get; set; }

        public PDUDeletePilot(string from, string cid)
            : base(from, "")
        {
            CID = cid;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("#DP");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(CID);
            return msg.ToString();
        }

        public static PDUDeletePilot Parse(string[] fields)
        {
            if (fields.Length < 1) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUDeletePilot(
                    fields[0],
                    fields.Length >= 2 ? fields[1] : ""
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
