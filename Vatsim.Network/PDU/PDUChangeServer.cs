using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUChangeServer : PDUBase
    {
        public string NewServer { get; set; }

        public PDUChangeServer(string from, string to, string newServer)
            : base(from, to)
        {
            NewServer = newServer;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("$XX");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(NewServer);
            return msg.ToString();
        }

        public static PDUChangeServer Parse(string[] fields)
        {
            if (fields.Length < 3)
            {
                throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            }

            try
            {
                return new PDUChangeServer(
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
