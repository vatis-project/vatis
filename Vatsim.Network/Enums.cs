namespace Vatsim.Network;

[Serializable]
public enum ProtocolRevision
{
    Unknown = 0,
    Classic = 9,
    VatsimNoAuth = 10,
    VatsimAuth = 100,
    Vatsim2022 = 101
}

[Serializable]
public enum VoiceSystem
{
    Afv,
    Vvl
}

[Serializable]
public enum ClientType
{
    Unknown,
    Pilot,
    Atc
}

[Serializable]
public enum NetworkError
{
    Ok,
    CallsignInUse,
    CallsignInvalid,
    AlreadyRegistered,
    SyntaxError,
    PduSourceInvalid,
    InvalidLogon,
    NoSuchCallsign,
    NoFlightPlan,
    NoWeatherProfile,
    InvalidProtocolRevision,
    RequestedLevelTooHigh,
    ServerFull,
    CertificateSuspended,
    InvalidControl,
    InvalidPositionForRating,
    UnauthorizedSoftware,
    AuthenticationResponseTimeout,
    InvalidClientVersion
}

[Serializable]
public enum NetworkRating
{
    Obs = 1,
    S1,
    S2,
    S3,
    C1,
    C2,
    C3,
    I1,
    I2,
    I3,
    Sup,
    Adm
}

[Serializable]
public enum NetworkFacility
{
    Obs,
    Fss,
    Del,
    Gnd,
    Twr,
    App,
    Ctr
}

[Serializable]
public enum SimulatorType
{
    Unknown,
    Msfs95,
    Msfs98,
    Mscfs,
    As2,
    Ps1,
    XPlane
}

[Serializable]
public enum FlightRules
{
    Ifr,
    Vfr,
    Dvfr,
    Svfr
}

public static class FlightRulesExtensions
{
    public static FlightRules FromString(this FlightRules _, string rulesString)
    {
        switch (rulesString.ToUpper())
        {
            case "I":
            case "IFR":
                return FlightRules.Ifr;
            case "V":
            case "VFR":
                return FlightRules.Vfr;
            case "D":
            case "DVFR":
                return FlightRules.Dvfr;
            case "S":
            case "SVFR":
                return FlightRules.Svfr;
            default:
                throw new ArgumentException($"Unknown flight rules: {rulesString}");
        }
    }

    public static string ToIcaoString(this FlightRules rules)
    {
        switch (rules)
        {
            case FlightRules.Ifr:
                return "I";
            case FlightRules.Vfr:
            case FlightRules.Dvfr:
            case FlightRules.Svfr:
                return "V";
            default:
                throw new ArgumentException($"Unknown flight rules: {rules}");
        }
    }
}

[Serializable]
public enum ControllerInfoType
{
    None,
    VoiceChannel,
    TextMessage,
    LogoffTime,
    LineCount,
    End
}

[Serializable]
public enum EngineType
{
    Piston,
    Jet,
    None,
    Helo
}

[Serializable]
public enum LandLineCommand
{
    Request,
    Approve,
    Reject,
    End
}

[Serializable]
public enum LandLineType
{
    Intercom,
    Override,
    Monitor
}

[Serializable]
public enum SharedStateType
{
    Unknown,
    Scratchpad,
    BeaconCode,
    VoiceType,
    TempAlt,
    GlobalData
}

[Serializable]
public enum ClientQueryType
{
    Unknown,
    IsValidAtc,
    Capabilities,
    Com1Freq,
    RealName,
    Server,
    Atis,
    PublicIp,
    Inf,
    FlightPlan,
    Ipc,
    RequestRelief,
    CancelRequestRelief,
    RequestHelp,
    CancelRequestHelp,
    WhoHas,
    InitiateTrack,
    AcceptHandoff,
    DropTrack,
    SetFinalAltitude,
    SetTempAltitude,
    SetBeaconCode,
    SetScratchpad,
    SetVoiceType,
    AircraftConfiguration,
    NewInfo,
    NewAtis,
    Estimate,
    SetGlobalData
}

public static class ClientQueryTypeExtensions
{
    public static ClientQueryType FromString(this ClientQueryType _, string typeId)
    {
        switch (typeId.ToUpper())
        {
            case "ATC":
                return ClientQueryType.IsValidAtc;
            case "CAPS":
                return ClientQueryType.Capabilities;
            case "C?":
                return ClientQueryType.Com1Freq;
            case "RN":
                return ClientQueryType.RealName;
            case "SV":
                return ClientQueryType.Server;
            case "ATIS":
                return ClientQueryType.Atis;
            case "IP":
                return ClientQueryType.PublicIp;
            case "INF":
                return ClientQueryType.Inf;
            case "FP":
                return ClientQueryType.FlightPlan;
            case "IPC":
                return ClientQueryType.Ipc;
            case "BY":
                return ClientQueryType.RequestRelief;
            case "HI":
                return ClientQueryType.CancelRequestRelief;
            case "HLP":
                return ClientQueryType.RequestHelp;
            case "NOHLP":
                return ClientQueryType.CancelRequestHelp;
            case "WH":
                return ClientQueryType.WhoHas;
            case "IT":
                return ClientQueryType.InitiateTrack;
            case "HT":
                return ClientQueryType.AcceptHandoff;
            case "DR":
                return ClientQueryType.DropTrack;
            case "FA":
                return ClientQueryType.SetFinalAltitude;
            case "TA":
                return ClientQueryType.SetTempAltitude;
            case "BC":
                return ClientQueryType.SetBeaconCode;
            case "SC":
                return ClientQueryType.SetScratchpad;
            case "VT":
                return ClientQueryType.SetVoiceType;
            case "ACC":
                return ClientQueryType.AircraftConfiguration;
            case "NEWINFO":
                return ClientQueryType.NewInfo;
            case "NEWATIS":
                return ClientQueryType.NewAtis;
            case "EST":
                return ClientQueryType.Estimate;
            case "GD":
                return ClientQueryType.SetGlobalData;
            default:
                return ClientQueryType.Unknown;
        }
    }

    public static string GetQueryTypeId(this ClientQueryType cq)
    {
        switch (cq)
        {
            case ClientQueryType.IsValidAtc:
                return "ATC";
            case ClientQueryType.Capabilities:
                return "CAPS";
            case ClientQueryType.Com1Freq:
                return "C?";
            case ClientQueryType.RealName:
                return "RN";
            case ClientQueryType.Server:
                return "SV";
            case ClientQueryType.Atis:
                return "ATIS";
            case ClientQueryType.PublicIp:
                return "IP";
            case ClientQueryType.Inf:
                return "INF";
            case ClientQueryType.FlightPlan:
                return "FP";
            case ClientQueryType.Ipc:
                return "IPC";
            case ClientQueryType.RequestRelief:
                return "BY";
            case ClientQueryType.CancelRequestRelief:
                return "HI";
            case ClientQueryType.RequestHelp:
                return "HLP";
            case ClientQueryType.CancelRequestHelp:
                return "NOHLP";
            case ClientQueryType.WhoHas:
                return "WH";
            case ClientQueryType.InitiateTrack:
                return "IT";
            case ClientQueryType.AcceptHandoff:
                return "HT";
            case ClientQueryType.DropTrack:
                return "DR";
            case ClientQueryType.SetFinalAltitude:
                return "FA";
            case ClientQueryType.SetTempAltitude:
                return "TA";
            case ClientQueryType.SetBeaconCode:
                return "BC";
            case ClientQueryType.SetScratchpad:
                return "SC";
            case ClientQueryType.SetVoiceType:
                return "VT";
            case ClientQueryType.AircraftConfiguration:
                return "ACC";
            case ClientQueryType.NewInfo:
                return "NEWINFO";
            case ClientQueryType.NewAtis:
                return "NEWATIS";
            case ClientQueryType.Estimate:
                return "EST";
            case ClientQueryType.SetGlobalData:
                return "GD";
            default:
                return "";
        }
    }
}
