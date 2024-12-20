using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUWindData : PDUBase
    {
        public int Layer1Ceiling { get; set; }
        public int Layer1Floor { get; set; }
        public int Layer1Direction { get; set; }
        public int Layer1Speed { get; set; }
        public bool Layer1Gusting { get; set; }
        public int Layer1Turbulence { get; set; }
        public int Layer2Ceiling { get; set; }
        public int Layer2Floor { get; set; }
        public int Layer2Direction { get; set; }
        public int Layer2Speed { get; set; }
        public bool Layer2Gusting { get; set; }
        public int Layer2Turbulence { get; set; }
        public int Layer3Ceiling { get; set; }
        public int Layer3Floor { get; set; }
        public int Layer3Direction { get; set; }
        public int Layer3Speed { get; set; }
        public bool Layer3Gusting { get; set; }
        public int Layer3Turbulence { get; set; }
        public int Layer4Ceiling { get; set; }
        public int Layer4Floor { get; set; }
        public int Layer4Direction { get; set; }
        public int Layer4Speed { get; set; }
        public bool Layer4Gusting { get; set; }
        public int Layer4Turbulence { get; set; }

        public PDUWindData(string from, string to, int layer1Ceiling, int layer1Floor, int layer1Direction, int layer1Speed, bool layer1Gusting, int layer1Turbulence, int layer2Ceiling, int layer2Floor, int layer2Direction, int layer2Speed, bool layer2Gusting, int layer2Turbulence, int layer3Ceiling, int layer3Floor, int layer3Direction, int layer3Speed, bool layer3Gusting, int layer3Turbulence, int layer4Ceiling, int layer4Floor, int layer4Direction, int layer4Speed, bool layer4Gusting, int layer4Turbulence)
            : base(from, to)
        {
            Layer1Ceiling = layer1Ceiling;
            Layer1Floor = layer1Floor;
            Layer1Direction = layer1Direction;
            Layer1Speed = layer1Speed;
            Layer1Gusting = layer1Gusting;
            Layer1Turbulence = layer1Turbulence;
            Layer2Ceiling = layer2Ceiling;
            Layer2Floor = layer2Floor;
            Layer2Direction = layer2Direction;
            Layer2Speed = layer2Speed;
            Layer2Gusting = layer2Gusting;
            Layer2Turbulence = layer2Turbulence;
            Layer3Ceiling = layer3Ceiling;
            Layer3Floor = layer3Floor;
            Layer3Direction = layer3Direction;
            Layer3Speed = layer3Speed;
            Layer3Gusting = layer3Gusting;
            Layer3Turbulence = layer3Turbulence;
            Layer4Ceiling = layer4Ceiling;
            Layer4Floor = layer4Floor;
            Layer4Direction = layer4Direction;
            Layer4Speed = layer4Speed;
            Layer4Gusting = layer4Gusting;
            Layer4Turbulence = layer4Turbulence;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("#WD");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(Layer1Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer1Floor);
            msg.Append(DELIMITER);
            msg.Append(Layer1Direction);
            msg.Append(DELIMITER);
            msg.Append(Layer1Speed);
            msg.Append(DELIMITER);
            msg.Append(Layer1Gusting ? "1" : "0");
            msg.Append(DELIMITER);
            msg.Append(Layer1Turbulence);
            msg.Append(DELIMITER);
            msg.Append(Layer2Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer2Floor);
            msg.Append(DELIMITER);
            msg.Append(Layer2Direction);
            msg.Append(DELIMITER);
            msg.Append(Layer2Speed);
            msg.Append(DELIMITER);
            msg.Append(Layer2Gusting ? "1" : "0");
            msg.Append(DELIMITER);
            msg.Append(Layer2Turbulence);
            msg.Append(DELIMITER);
            msg.Append(Layer3Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer3Floor);
            msg.Append(DELIMITER);
            msg.Append(Layer3Direction);
            msg.Append(DELIMITER);
            msg.Append(Layer3Speed);
            msg.Append(DELIMITER);
            msg.Append(Layer3Gusting ? "1" : "0");
            msg.Append(DELIMITER);
            msg.Append(Layer3Turbulence);
            msg.Append(DELIMITER);
            msg.Append(Layer4Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer4Floor);
            msg.Append(DELIMITER);
            msg.Append(Layer4Direction);
            msg.Append(DELIMITER);
            msg.Append(Layer4Speed);
            msg.Append(DELIMITER);
            msg.Append(Layer4Gusting ? "1" : "0");
            msg.Append(DELIMITER);
            msg.Append(Layer4Turbulence);
            return msg.ToString();
        }

        public static PDUWindData Parse(string[] fields)
        {
            if (fields.Length < 26) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUWindData(
                    fields[0],
                    fields[1],
                    Convert.ToInt32(fields[2]),
                    Convert.ToInt32(fields[3]),
                    Convert.ToInt32(fields[4]),
                    Convert.ToInt32(fields[5]),
                    fields[6].Equals("1"),
                    Convert.ToInt32(fields[7]),
                    Convert.ToInt32(fields[8]),
                    Convert.ToInt32(fields[9]),
                    Convert.ToInt32(fields[10]),
                    Convert.ToInt32(fields[11]),
                    fields[12].Equals("1"),
                    Convert.ToInt32(fields[13]),
                    Convert.ToInt32(fields[14]),
                    Convert.ToInt32(fields[15]),
                    Convert.ToInt32(fields[16]),
                    Convert.ToInt32(fields[17]),
                    fields[18].Equals("1"),
                    Convert.ToInt32(fields[19]),
                    Convert.ToInt32(fields[20]),
                    Convert.ToInt32(fields[21]),
                    Convert.ToInt32(fields[22]),
                    Convert.ToInt32(fields[23]),
                    fields[24].Equals("1"),
                    Convert.ToInt32(fields[25])
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
