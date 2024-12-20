using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUCloudData : PDUBase
    {
        public int Layer1Ceiling { get; set; }
        public int Layer1Floor { get; set; }
        public int Layer1Coverage { get; set; }
        public bool Layer1Icing { get; set; }
        public int Layer1Turbulence { get; set; }
        public int Layer2Ceiling { get; set; }
        public int Layer2Floor { get; set; }
        public int Layer2Coverage { get; set; }
        public bool Layer2Icing { get; set; }
        public int Layer2Turbulence { get; set; }
        public int StormLayerCeiling { get; set; }
        public int StormLayerFloor { get; set; }
        public int StormLayerDeviation { get; set; }
        public int StormLayerCoverage { get; set; }
        public int StormLayerTurbulence { get; set; }

        public PDUCloudData(string from, string to, int layer1Ceiling, int layer1Floor, int layer1Coverage, bool layer1Icing, int layer1Turbulence, int layer2Ceiling, int layer2Floor, int layer2Coverage, bool layer2Icing, int layer2Turbulence, int stormLayerCeiling, int stormLayerFloor, int stormLayerDeviation, int stormLayerCoverage, int stormLayerTurbulence)
            : base(from, to)
        {
            Layer1Ceiling = layer1Ceiling;
            Layer1Floor = layer1Floor;
            Layer1Coverage = layer1Coverage;
            Layer1Icing = layer1Icing;
            Layer1Turbulence = layer1Turbulence;
            Layer2Ceiling = layer2Ceiling;
            Layer2Floor = layer2Floor;
            Layer2Coverage = layer2Coverage;
            Layer2Icing = layer2Icing;
            Layer2Turbulence = layer2Turbulence;
            StormLayerCeiling = stormLayerCeiling;
            StormLayerFloor = stormLayerFloor;
            StormLayerDeviation = stormLayerDeviation;
            StormLayerCoverage = stormLayerCoverage;
            StormLayerTurbulence = stormLayerTurbulence;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("#CD");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(To);
            msg.Append(DELIMITER);
            msg.Append(Layer1Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer1Floor);
            msg.Append(DELIMITER);
            msg.Append(Layer1Coverage);
            msg.Append(DELIMITER);
            msg.Append(Layer1Icing ? "1" : "0");
            msg.Append(DELIMITER);
            msg.Append(Layer1Turbulence);
            msg.Append(DELIMITER);
            msg.Append(Layer2Ceiling);
            msg.Append(DELIMITER);
            msg.Append(Layer2Floor);
            msg.Append(DELIMITER);
            msg.Append(Layer2Coverage);
            msg.Append(DELIMITER);
            msg.Append(Layer2Icing ? "1" : "0");
            msg.Append(DELIMITER);
            msg.Append(Layer2Turbulence);
            msg.Append(DELIMITER);
            msg.Append(StormLayerCeiling);
            msg.Append(DELIMITER);
            msg.Append(StormLayerFloor);
            msg.Append(DELIMITER);
            msg.Append(StormLayerDeviation);
            msg.Append(DELIMITER);
            msg.Append(StormLayerCoverage);
            msg.Append(DELIMITER);
            msg.Append(StormLayerTurbulence);
            return msg.ToString();
        }

        public static PDUCloudData Parse(string[] fields)
        {
            if (fields.Length < 17) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            try
            {
                return new PDUCloudData(
                    fields[0],
                    fields[1],
                    Convert.ToInt32(fields[2]),
                    Convert.ToInt32(fields[3]),
                    Convert.ToInt32(fields[4]),
                    fields[5].Equals("1"),
                    Convert.ToInt32(fields[6]),
                    Convert.ToInt32(fields[7]),
                    Convert.ToInt32(fields[8]),
                    Convert.ToInt32(fields[9]),
                    fields[10].Equals("1"),
                    Convert.ToInt32(fields[11]),
                    Convert.ToInt32(fields[12]),
                    Convert.ToInt32(fields[13]),
                    Convert.ToInt32(fields[14]),
                    Convert.ToInt32(fields[15]),
                    Convert.ToInt32(fields[16])
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
