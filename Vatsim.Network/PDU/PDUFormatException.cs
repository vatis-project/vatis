namespace Vatsim.Network.PDU
{
    public class PDUFormatException : Exception
    {
        public string RawMessage { get; set; }

        public PDUFormatException(string error, string rawMessage)
            : this(error, rawMessage, null)
        {
        }

        public PDUFormatException(string error, string rawMessage, Exception? innerException = null)
            : base(error, innerException)
        {
            RawMessage = rawMessage;
        }
    }
}
