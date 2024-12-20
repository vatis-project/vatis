using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUClientQuery : PDUBase
    {
        public ClientQueryType QueryType { get; set; }
        public List<string>? Payload { get; set; }

        public PDUClientQuery(string from, string to, ClientQueryType queryType)
            : this(from, to, queryType, null)
        {
        }

        public PDUClientQuery(string from, string to, ClientQueryType queryType, List<string>? payload = null)
            : base(from, to)
        {
            QueryType = queryType;
            Payload = payload;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("$CQ");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(QueryType.GetQueryTypeID());
            if (Payload != null)
            {
                foreach (string payloadItem in Payload)
                {
                    msg.Append(DELIMITER);
                    msg.Append(payloadItem);
                }
            }
            return msg.ToString();
        }

        public static PDUClientQuery Parse(string[] fields)
        {
            if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                ClientQueryType queryType = ClientQueryType.Unknown;
                List<string> payload = new List<string>();
                if (fields.Length > 3)
                {
                    for (int i = 3; i < fields.Length; i++)
                    {
                        payload.Add(fields[i]);
                    }
                }
                return new PDUClientQuery(
                    fields[0],
                    fields[1],
                    queryType.FromString(fields[2]),
                    payload
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
