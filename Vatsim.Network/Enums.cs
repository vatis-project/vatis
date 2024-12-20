namespace Vatsim.Network
{
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
        ATC
    }

    [Serializable]
    public enum NetworkError
    {
        Ok,
        CallsignInUse,
        CallsignInvalid,
        AlreadyRegistered,
        SyntaxError,
        PDUSourceInvalid,
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
        OBS = 1,
        S1,
        S2,
        S3,
        C1,
        C2,
        C3,
        I1,
        I2,
        I3,
        SUP,
        ADM
    }

    [Serializable]
    public enum NetworkFacility
    {
        OBS,
        FSS,
        DEL,
        GND,
        TWR,
        APP,
        CTR
    }

    [Serializable]
    public enum SimulatorType
    {
        Unknown,
        MSFS95,
        MSFS98,
        MSCFS,
        AS2,
        PS1,
        XPlane
    }

    [Serializable]
    public enum FlightRules
    {
        IFR,
        VFR,
        DVFR,
        SVFR
    }

    public static class FlightRulesExtensions
    {
        public static FlightRules FromString(this FlightRules _, string rulesString)
        {
            switch (rulesString.ToUpper())
            {
                case "I":
                case "IFR":
                    return FlightRules.IFR;
                case "V":
                case "VFR":
                    return FlightRules.VFR;
                case "D":
                case "DVFR":
                    return FlightRules.DVFR;
                case "S":
                case "SVFR":
                    return FlightRules.SVFR;
                default:
                    throw new ArgumentException(string.Format("Unknown flight rules: {0}", rulesString));
            }
        }

        public static string ToICAOString(this FlightRules rules)
        {
            switch (rules)
            {
                case FlightRules.IFR:
                    return "I";
                case FlightRules.VFR:
                case FlightRules.DVFR:
                case FlightRules.SVFR:
                    return "V";
                default:
                    throw new ArgumentException(string.Format("Unknown flight rules: {0}", rules));
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
        IsValidATC,
        Capabilities,
        COM1Freq,
        RealName,
        Server,
        ATIS,
        PublicIP,
        INF,
        FlightPlan,
        IPC,
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
        NewATIS,
        Estimate,
        SetGlobalData
    }

    public static class ClientQueryTypeExtensions
    {
        public static ClientQueryType FromString(this ClientQueryType _, string typeID)
        {
            switch (typeID.ToUpper())
            {
                case "ATC":
                    return ClientQueryType.IsValidATC;
                case "CAPS":
                    return ClientQueryType.Capabilities;
                case "C?":
                    return ClientQueryType.COM1Freq;
                case "RN":
                    return ClientQueryType.RealName;
                case "SV":
                    return ClientQueryType.Server;
                case "ATIS":
                    return ClientQueryType.ATIS;
                case "IP":
                    return ClientQueryType.PublicIP;
                case "INF":
                    return ClientQueryType.INF;
                case "FP":
                    return ClientQueryType.FlightPlan;
                case "IPC":
                    return ClientQueryType.IPC;
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
                    return ClientQueryType.NewATIS;
                case "EST":
                    return ClientQueryType.Estimate;
                case "GD":
                    return ClientQueryType.SetGlobalData;
                default:
                    return ClientQueryType.Unknown;
            }
        }

        public static string GetQueryTypeID(this ClientQueryType cq)
        {
            switch (cq)
            {
                case ClientQueryType.IsValidATC:
                    return "ATC";
                case ClientQueryType.Capabilities:
                    return "CAPS";
                case ClientQueryType.COM1Freq:
                    return "C?";
                case ClientQueryType.RealName:
                    return "RN";
                case ClientQueryType.Server:
                    return "SV";
                case ClientQueryType.ATIS:
                    return "ATIS";
                case ClientQueryType.PublicIP:
                    return "IP";
                case ClientQueryType.INF:
                    return "INF";
                case ClientQueryType.FlightPlan:
                    return "FP";
                case ClientQueryType.IPC:
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
                case ClientQueryType.NewATIS:
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
}
