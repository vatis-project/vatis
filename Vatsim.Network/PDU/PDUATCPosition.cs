using System.Globalization;
using System.Text;

namespace Vatsim.Network.PDU
{
    public class PDUATCPosition : PDUBase
    {
        public int[] Frequencies { get; set; }
        public NetworkFacility Facility { get; set; }
        public int VisibilityRange { get; set; }
        public NetworkRating Rating { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }

        public int Frequency
        {
            get
            {
                return Frequencies[0];
            }
            set
            {
                if (Frequencies == null || Frequencies.Length == 0)
                {
                    Frequencies = new int[1] { value };
                }
                else
                {
                    Frequencies[0] = value;
                }
            }
        }

        public PDUATCPosition(string from, int freq, NetworkFacility facility, int visRange, NetworkRating rating, double lat, double lon)
            : this(from, new int[1] { freq }, facility, visRange, rating, lat, lon)
        {
        }

        public PDUATCPosition(string from, int[] freqs, NetworkFacility facility, int visRange, NetworkRating rating, double lat, double lon)
            : base(from, "")
        {
            if (double.IsNaN(lat))
            {
                throw new ArgumentException("Latitude must be a valid double precision number.", "lat");
            }
            if (double.IsNaN(lon))
            {
                throw new ArgumentException("Longitude must be a valid double precision number.", "lon");
            }
            Frequencies = freqs;
            Facility = facility;
            VisibilityRange = visRange;
            Rating = rating;
            Lat = lat;
            Lon = lon;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder("%");
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(string.Join("&", Frequencies.Select(f => f.ToString()).ToArray()));
            msg.Append(DELIMITER);
            msg.Append((int)Facility);
            msg.Append(DELIMITER);
            msg.Append(VisibilityRange);
            msg.Append(DELIMITER);
            msg.Append((int)Rating);
            msg.Append(DELIMITER);
            msg.Append(Lat.ToString("#0.00000", CultureInfo.InvariantCulture));
            msg.Append(DELIMITER);
            msg.Append(Lon.ToString("#0.00000", CultureInfo.InvariantCulture));
            msg.Append(DELIMITER);
            msg.Append("0");
            return msg.ToString();
        }

        public static PDUATCPosition Parse(string[] fields)
        {
            if (fields.Length < 7)
            {
                throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            }

            try
            {
                return new PDUATCPosition(
                    fields[0],
                    fields[1].Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).Select(f => int.Parse(f)).ToArray(),
                    (NetworkFacility)int.Parse(fields[2]),
                    int.Parse(fields[3]),
                    (NetworkRating)int.Parse(fields[4]),
                    double.Parse(fields[5], CultureInfo.InvariantCulture),
                    double.Parse(fields[6], CultureInfo.InvariantCulture)
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
