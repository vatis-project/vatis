using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUTemperatureData : PDUBase
    {
        public int Layer1Ceiling { get; set; }
        public int Layer1Temperature { get; set; }
        public int Layer2Ceiling { get; set; }
        public int Layer2Temperature { get; set; }
        public int Layer3Ceiling { get; set; }
        public int Layer3Temperature { get; set; }
        public int Layer4Ceiling { get; set; }
        public int Layer4Temperature { get; set; }
        public int Pressure { get; set; }

        public PDUTemperatureData(string from, string to, int layer1Ceiling, int layer1Temperature, int layer2Ceiling, int layer2Temperature, int layer3Ceiling, int layer3Temperature, int layer4Ceiling, int layer4Temperature, int pressure)
            : base(from, to)
        {
            Layer1Ceiling = layer1Ceiling;
            Layer1Temperature = layer1Temperature;
            Layer2Ceiling = layer2Ceiling;
            Layer2Temperature = layer2Temperature;
            Layer3Ceiling = layer3Ceiling;
            Layer3Temperature = layer3Temperature;
            Layer4Ceiling = layer4Ceiling;
            Layer4Temperature = layer4Temperature;
            Pressure = pressure;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("#TD");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(Layer1Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer1Temperature);
            msg.Append(DELIMITER);
            msg.Append(Layer2Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer2Temperature);
            msg.Append(DELIMITER);
            msg.Append(Layer3Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer3Temperature);
            msg.Append(DELIMITER);
            msg.Append(Layer4Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer4Temperature);
            msg.Append(DELIMITER);
            msg.Append(Pressure);
            return msg.ToString();
        }

        public static PDUTemperatureData Parse(string[] fields)
        {
            if (fields.Length < 11) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUTemperatureData(
                    fields[0],
                    fields[1],
                    Convert.ToInt32(fields[2]),
                    Convert.ToInt32(fields[3]),
                    Convert.ToInt32(fields[4]),
                    Convert.ToInt32(fields[5]),
                    Convert.ToInt32(fields[6]),
                    Convert.ToInt32(fields[7]),
                    Convert.ToInt32(fields[8]),
                    Convert.ToInt32(fields[9]),
                    Convert.ToInt32(fields[10])
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
