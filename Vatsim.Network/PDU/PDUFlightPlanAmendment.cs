using System.Text;

namespace Vatsim.Network.PDU;

public class PDUFlightPlanAmendment : PDUFlightPlan
{
    public string Callsign { get; set; }

    public PDUFlightPlanAmendment(string from, string to, string callsign, FlightRules rules, string equipment,
        string tas, string depAirport, string estimatedDepTime, string actualDepTime, string cruiseAlt,
        string destAirport, string hoursEnroute, string minutesEnroute, string fuelAvailHours,
        string fuelAvailMinutes, string altAirport, string remarks, string route)
        : base(from, to, rules, equipment, tas, depAirport, estimatedDepTime, actualDepTime, cruiseAlt, destAirport, hoursEnroute, minutesEnroute, fuelAvailHours, fuelAvailMinutes, altAirport, remarks, route)
    {
        Callsign = callsign;
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("$AM");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
        msg.Append(DELIMITER);
        msg.Append(Callsign);
        msg.Append(DELIMITER);
        msg.Append(Rules.ToString().Substring(0, 1));
        msg.Append(DELIMITER);
        msg.Append(Equipment);
        msg.Append(DELIMITER);
        msg.Append(TAS);
        msg.Append(DELIMITER);
        msg.Append(DepAirport);
        msg.Append(DELIMITER);
        msg.Append(EstimatedDepTime);
        msg.Append(DELIMITER);
        msg.Append(ActualDepTime);
        msg.Append(DELIMITER);
        msg.Append(CruiseAlt);
        msg.Append(DELIMITER);
        msg.Append(DestAirport);
        msg.Append(DELIMITER);
        msg.Append(HoursEnroute);
        msg.Append(DELIMITER);
        msg.Append(MinutesEnroute);
        msg.Append(DELIMITER);
        msg.Append(FuelAvailHours);
        msg.Append(DELIMITER);
        msg.Append(FuelAvailMinutes);
        msg.Append(DELIMITER);
        msg.Append(AltAirport);
        msg.Append(DELIMITER);
        msg.Append(Remarks);
        msg.Append(DELIMITER);
        msg.Append(Route);
        return msg.ToString();
    }

    public new static PDUFlightPlanAmendment Parse(string[] fields)
    {
        if (fields.Length < 18) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            FlightRules fr = FlightRules.Ifr;
            return new PDUFlightPlanAmendment(
                fields[0],
                fields[1],
                fields[2],
                fr.FromString(fields[3]),
                fields[4],
                fields[5],
                fields[6],
                fields[7],
                fields[8],
                fields[9],
                fields[10],
                fields[11],
                fields[12],
                fields[13],
                fields[14],
                fields[15],
                fields[16],
                fields[17]
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}