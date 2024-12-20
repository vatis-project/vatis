using System.Globalization;
using System.Text;

namespace Vatsim.Network.PDU
{
    [Serializable]
    public enum FastPilotPositionType
    {
        Fast,
        Slow,
        Stopped
    }

    public static class FastPilotPositionTypeExtensions
    {
        public static int FieldCount(this FastPilotPositionType type)
        {
            switch (type)
            {
                case FastPilotPositionType.Fast: return 12;
                case FastPilotPositionType.Slow: return 12;
                case FastPilotPositionType.Stopped: return 6;
                default: throw new ApplicationException($"Unknown fast pilot position type: {type}.");
            }
        }

        public static string Prefix(this FastPilotPositionType type)
        {
            switch (type)
            {
                case FastPilotPositionType.Fast: return "^";
                case FastPilotPositionType.Slow: return "#SL";
                case FastPilotPositionType.Stopped: return "#ST";
                default: throw new ApplicationException($"Unknown fast pilot position type: {type}.");
            }
        }
    }

    public class PDUFastPilotPosition : PDUBase
    {
        // Fast: ^CALLSIGN:lat:lon:alt:agl:pbh:velx:vely:velz:velp:velh:velb:noseangle(optional)
        // Slow: #SLCALLSIGN:lat:lon:alt:agl:pbh:velx:vely:velz:velp:velh:velb:noseangle(optional)
        // Stopped: #STCALLSIGN:lat:lon:alt:agl:pbh:noseangle(optional)

        public FastPilotPositionType Type { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double AltitudeTrue { get; set; }
        public double AltitudeAgl { get; set; }
        public double Pitch { get; set; }
        public double Heading { get; set; }
        public double Bank { get; set; }
        public double VelocityLongitude { get; set; }
        public double VelocityAltitude { get; set; }
        public double VelocityLatitude { get; set; }
        public double VelocityPitch { get; set; }
        public double VelocityHeading { get; set; }
        public double VelocityBank { get; set; }
        public double NoseGearAngle { get; set; }

        public PDUFastPilotPosition(
            FastPilotPositionType type,
            string from,
            double lat,
            double lon,
            double altTrue,
            double altAgl,
            double pitch,
            double heading,
            double bank,
            double velocityLongitude,
            double velocityAltitude,
            double velocityLatitude,
            double velocityPitch,
            double velocityHeading,
            double velocityBank,
            double noseGearAngle
        )
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

            Type = type;
            Lat = lat;
            Lon = lon;
            AltitudeTrue = altTrue;
            AltitudeAgl = altAgl;
            Pitch = pitch;
            Heading = heading;
            Bank = bank;
            VelocityLongitude = velocityLongitude;
            VelocityAltitude = velocityAltitude;
            VelocityLatitude = velocityLatitude;
            VelocityPitch = velocityPitch;
            VelocityHeading = velocityHeading;
            VelocityBank = velocityBank;
            NoseGearAngle = noseGearAngle;
        }

        public override string Serialize()
        {
            StringBuilder msg = new StringBuilder(Type.Prefix());
            msg.Append(From);
            msg.Append(DELIMITER);
            msg.Append(Lat.ToString("#0.0000000", CultureInfo.InvariantCulture));
            msg.Append(DELIMITER);
            msg.Append(Lon.ToString("#0.0000000", CultureInfo.InvariantCulture));
            msg.Append(DELIMITER);
            msg.Append(AltitudeTrue.ToString("#0.00", CultureInfo.InvariantCulture));
            msg.Append(DELIMITER);
            msg.Append(AltitudeAgl.ToString("#0.00", CultureInfo.InvariantCulture));
            msg.Append(DELIMITER);
            msg.Append(PackPitchBankHeading(Pitch, Bank, Heading));
            if (Type != FastPilotPositionType.Stopped)
            {
                msg.Append(DELIMITER);
                msg.Append(VelocityLongitude.ToString("#0.0000", CultureInfo.InvariantCulture));
                msg.Append(DELIMITER);
                msg.Append(VelocityAltitude.ToString("#0.0000", CultureInfo.InvariantCulture));
                msg.Append(DELIMITER);
                msg.Append(VelocityLatitude.ToString("#0.0000", CultureInfo.InvariantCulture));
                msg.Append(DELIMITER);
                msg.Append(VelocityPitch.ToString("#0.0000", CultureInfo.InvariantCulture));
                msg.Append(DELIMITER);
                msg.Append(VelocityHeading.ToString("#0.0000", CultureInfo.InvariantCulture));
                msg.Append(DELIMITER);
                msg.Append(VelocityBank.ToString("#0.0000", CultureInfo.InvariantCulture));
            }
            msg.Append(DELIMITER);
            msg.Append(NoseGearAngle.ToString("#0.00", CultureInfo.InvariantCulture));
            return msg.ToString();
        }

        public static PDUFastPilotPosition Parse(FastPilotPositionType type, string[] fields)
        {
            if (fields.Length < type.FieldCount())
            {
                throw new PDUFormatException("Invalid field count.", Reassemble(fields));
            }

            try
            {
                UnpackPitchBankHeading(uint.Parse(fields[5]), out double pitch, out double bank, out double heading);
                string from = fields[0];
                double lat = double.Parse(fields[1], CultureInfo.InvariantCulture);
                double lon = double.Parse(fields[2], CultureInfo.InvariantCulture);
                double altTrue = double.Parse(fields[3], CultureInfo.InvariantCulture);
                double altAgl = double.Parse(fields[4], CultureInfo.InvariantCulture);
                double velLon = 0.0;
                double velAlt = 0.0;
                double velLat = 0.0;
                double velPitch = 0.0;
                double velHeading = 0.0;
                double velBank = 0.0;
                double noseGearAngle;
                if (type != FastPilotPositionType.Stopped)
                {
                    velLon = double.Parse(fields[6], CultureInfo.InvariantCulture);
                    velAlt = double.Parse(fields[7], CultureInfo.InvariantCulture);
                    velLat = double.Parse(fields[8], CultureInfo.InvariantCulture);
                    velPitch = double.Parse(fields[9], CultureInfo.InvariantCulture);
                    velHeading = double.Parse(fields[10], CultureInfo.InvariantCulture);
                    velBank = double.Parse(fields[11], CultureInfo.InvariantCulture);
                    noseGearAngle = fields.Length >= 13 ? double.Parse(fields[12], CultureInfo.InvariantCulture) : 0.0;
                }
                else
                {
                    noseGearAngle = fields.Length >= 7 ? double.Parse(fields[6], CultureInfo.InvariantCulture) : 0.0;
                }
                return new PDUFastPilotPosition(
                    type,
                    from,
                    lat,
                    lon,
                    altTrue,
                    altAgl,
                    pitch,
                    heading,
                    bank,
                    velLon,
                    velAlt,
                    velLat,
                    velPitch,
                    velHeading,
                    velBank,
                    noseGearAngle
                );
            }
            catch (Exception ex)
            {
                throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
            }
        }
    }
}
