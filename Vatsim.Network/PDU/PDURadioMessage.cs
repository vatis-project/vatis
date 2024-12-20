using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDURadioMessage : PDUBase
    {
        public int[] Frequencies { get; set; }
        public string Message { get; set; }

        public PDURadioMessage(string from, int[] freqs, string message)
            : base(from, "")
        {
            Frequencies = freqs;
            Message = message;
        }

        public override string Serialize()
        {
            StringBuilder freqs = new StringBuilder();
            foreach (int freq in Frequencies)
            {
                if (freqs.Length > 0) freqs.Append("&");
                freqs.AppendFormat("@{0}", freq.ToString());
            }
            StringBuilder msg = new StringBuilder("#TM");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(freqs.ToString());
            msg.Append(DELIMITER);
            msg.Append(Message);
            return msg.ToString();
        }

        public static PDURadioMessage Parse(string[] fields)
        {
            if (fields.Length < 3) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            string[] freqs = fields[1].Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            int[] freqInts = new int[freqs.Length];
            for (int i = 0; i < freqs.Length; i++)
            {
                freqInts[i] = int.Parse(freqs[i].Substring(1));
            }
            StringBuilder msg = new StringBuilder(fields[2]);
            for (int i = 3; i < fields.Length; i++) msg.AppendFormat(":{0}", fields[i]);
            try
            {
                return new PDURadioMessage(
                    fields[0],
                    freqInts,
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
