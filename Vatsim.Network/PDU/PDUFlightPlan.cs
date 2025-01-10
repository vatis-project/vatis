using System.Text;

namespace Vatsim.Network.PDU;

public class PDUFlightPlan : PDUBase
{
    public FlightRules Rules { get; set; }
    public string Equipment { get; set; }
    public string TAS { get; set; }
    public string DepAirport { get; set; }
    public string EstimatedDepTime { get; set; }
    public string ActualDepTime { get; set; }
    public string CruiseAlt { get; set; }
    public string DestAirport { get; set; }
    public string HoursEnroute { get; set; }
    public string MinutesEnroute { get; set; }
    public string FuelAvailHours { get; set; }
    public string FuelAvailMinutes { get; set; }
    public string AltAirport { get; set; }
    public string Remarks { get; set; }
    public string Route { get; set; }

    public PDUFlightPlan(string from, string to, FlightRules rules, string equipment, string tas,
        string depAirport, string estimatedDepTime, string actualDepTime, string cruiseAlt,
        string destAirport, string hoursEnroute, string minutesEnroute, string fuelAvailHours,
        string fuelAvailMinutes, string altAirport, string remarks, string route)
        : base(from, to)
    {
        Rules = rules;
        Equipment = equipment;
        TAS = tas;
        DepAirport = depAirport;
        EstimatedDepTime = estimatedDepTime;
        ActualDepTime = actualDepTime;
        CruiseAlt = cruiseAlt;
        DestAirport = destAirport;
        HoursEnroute = hoursEnroute;
        MinutesEnroute = minutesEnroute;
        FuelAvailHours = fuelAvailHours;
        FuelAvailMinutes = fuelAvailMinutes;
        AltAirport = altAirport;
        Remarks = remarks.Replace(":", " ");
        Route = route.Replace(":", " ");
    }

    public override string Serialize()
    {
        StringBuilder msg = new StringBuilder("$FP");
        msg.Append(From);
        msg.Append(DELIMITER);
        msg.Append(To);
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

    public static PDUFlightPlan Parse(string[] fields)
    {
        if (fields.Length < 17) throw new PDUFormatException("Invalid field count.", Reassemble(fields));
        try
        {
            FlightRules fr = FlightRules.Ifr;
            return new PDUFlightPlan(
                fields[0],
                fields[1],
                fr.FromString(fields[2]),
                fields[3],
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
                fields[16]
            );
        }
        catch (Exception ex)
        {
            throw new PDUFormatException("Parse error.", Reassemble(fields), ex);
        }
    }
}