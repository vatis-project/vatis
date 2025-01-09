using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Vatsim.Network.PDU;

namespace Vatsim.Network;

public class FsdSession
{
    private const int ServerAuthChallengeInterval = 60000;
    private const int ServerAuthChallengeResponseWindow = 30000;

    public event EventHandler<NetworkEventArgs> NetworkConnected = delegate { };
    public event EventHandler<NetworkEventArgs> NetworkDisconnected = delegate { };
    public event EventHandler<NetworkEventArgs> NetworkConnectionFailed = delegate { };
    public event EventHandler<NetworkErrorEventArgs> NetworkError = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUATCPosition>> AtcPositionReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUPilotPosition>> PilotPositionReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUFastPilotPosition>> FastPilotPositionReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUSecondaryVisCenter>> SecondaryVisCenterReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUClientIdentification>> ClientIdentificationReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUServerIdentification>> ServerIdentificationReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUAddATC>> AddAtcReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUDeleteATC>> DeleteAtcReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUAddPilot>> AddPilotReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUDeletePilot>> DeletePilotReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUTextMessage>> TextMessageReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUATCMessage>> AtcMessageReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDURadioMessage>> RadioMessageReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUBroadcastMessage>> BroadcastMessageReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUWallop>> WallopReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUWeatherProfileRequest>> WeatherProfileRequestReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUWindData>> WindDataReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUTemperatureData>> TemperatureDataReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUCloudData>> CloudDataReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUHandoffCancelled>> HandoffCancelledReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUFlightStrip>> FlightStripReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUPushToDepartureList>> PushToDepartureListReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUPointout>> PointoutReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUIHaveTarget>> IHaveTargetReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUSharedState>> SharedStateReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDULandLineCommand>> LandLineCommandReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUPlaneInfoRequest>> PlaneInfoRequestReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUPlaneInfoResponse>> PlaneInfoResponseReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDULegacyPlaneInfoResponse>> LegacyPlaneInfoResponseReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUFlightPlan>> FlightPlanReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUFlightPlanAmendment>> FlightPlanAmendmentReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUPing>> PingReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUPong>> PongReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUHandoff>> HandoffReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUHandoffAccept>> HandoffAcceptReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUMetarRequest>> AcarsQueryReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUMetarResponse>> AcarsResponseReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUClientQuery>> ClientQueryReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUClientQueryResponse>> ClientQueryResponseReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUAuthChallenge>> AuthChallengeReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUAuthResponse>> AuthResponseReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUKillRequest>> KillRequestReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUProtocolError>> ProtocolErrorReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUVersionRequest>> VersionRequestReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUSendFastPositions>> SendFastPositionsReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUChangeServer>> ChangeServerReceived = delegate { };
    public event EventHandler<DataReceivedEventArgs<PDUMute>> MuteReceived = delegate { };
    public event EventHandler<RawDataEventArgs> RawDataSent = delegate { };
    public event EventHandler<RawDataEventArgs> RawDataReceived = delegate { };

    private Socket? _clientSocket;
    private AsyncCallback? _incomingDataCallBack;
    private string _partialPacket = "";
    private string _clientAuthSessionKey = "";
    private string _clientAuthChallengeKey = "";
    private readonly object? _userData;
    private SynchronizationContext? _syncContext;
    private bool _challengeServer;
    private string _serverAuthSessionKey = string.Empty;
    private string _serverAuthChallengeKey = string.Empty;
    private string _lastServerAuthChallenge = string.Empty;
    private Timer? _serverAuthTimer;
    private string? _currentCallsign;
    private readonly IClientAuth _clientAuth;

    public bool Connected => _clientSocket?.Connected ?? false;
    public bool IgnoreUnknownPackets { get; init; }
    public ClientProperties ClientProperties { get; set; }

    private FsdSession(ClientProperties properties, object? userData, SynchronizationContext? syncContext)
    {
        ClientProperties = properties;
        _userData = userData;
        _syncContext = syncContext;
        _clientAuth = new ClientAuth();
    }

    public FsdSession(ClientProperties properties, object userData)
        : this(properties, userData, null)
    {
    }

    public FsdSession(ClientProperties properties, SynchronizationContext syncContext)
        : this(properties, null, syncContext)
    {
    }

    public FsdSession(ClientProperties properties)
        : this(properties, null, null)
    {
    }

    private void RaiseNetworkConnected()
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                NetworkConnected(this, new NetworkEventArgs(_userData));
            }, null);
        }
        else
        {
            NetworkConnected(this, new NetworkEventArgs(_userData));
        }
    }

    private void RaiseNetworkDisconnected()
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                NetworkDisconnected(this, new NetworkEventArgs(_userData));
            }, null);
        }
        else
        {
            NetworkDisconnected(this, new NetworkEventArgs(_userData));
        }
    }

    private void RaiseNetworkConnectionFailed()
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                NetworkConnectionFailed(this, new NetworkEventArgs(_userData));
            }, null);
        }
        else
        {
            NetworkConnectionFailed(this, new NetworkEventArgs(_userData));
        }
    }

    private void RaiseNetworkError(string message)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                NetworkError(this, new NetworkErrorEventArgs(message, _userData));
            }, null);
        }
        else
        {
            NetworkError(this, new NetworkErrorEventArgs(message, _userData));
        }
    }

    private void RaiseRawDataSent(string data)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                RawDataSent(this, new RawDataEventArgs(data, _userData));
            }, null);
        }
        else
        {
            RawDataSent(this, new RawDataEventArgs(data, _userData));
        }
    }

    private void RaiseRawDataReceived(string data)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                RawDataReceived(this, new RawDataEventArgs(data, _userData));
            }, null);
        }
        else
        {
            RawDataReceived(this, new RawDataEventArgs(data, _userData));
        }
    }

    private void RaiseAtcPositionReceived(PDUATCPosition pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                AtcPositionReceived(this, new DataReceivedEventArgs<PDUATCPosition>(pdu, _userData));
            }, null);
        }
        else
        {
            AtcPositionReceived(this, new DataReceivedEventArgs<PDUATCPosition>(pdu, _userData));
        }
    }

    private void RaisePilotPositionReceived(PDUPilotPosition pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                PilotPositionReceived(this, new DataReceivedEventArgs<PDUPilotPosition>(pdu, _userData));
            }, null);
        }
        else
        {
            PilotPositionReceived(this, new DataReceivedEventArgs<PDUPilotPosition>(pdu, _userData));
        }
    }

    private void RaiseFastPilotPositionReceived(PDUFastPilotPosition pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                FastPilotPositionReceived(this, new DataReceivedEventArgs<PDUFastPilotPosition>(pdu, _userData));
            }, null);
        }
        else
        {
            FastPilotPositionReceived(this, new DataReceivedEventArgs<PDUFastPilotPosition>(pdu, _userData));
        }
    }

    private void RaiseSecondaryVisCenterReceived(PDUSecondaryVisCenter pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                SecondaryVisCenterReceived(this, new DataReceivedEventArgs<PDUSecondaryVisCenter>(pdu, _userData));
            }, null);
        }
        else
        {
            SecondaryVisCenterReceived(this, new DataReceivedEventArgs<PDUSecondaryVisCenter>(pdu, _userData));
        }
    }

    private void RaiseClientIdentificationReceived(PDUClientIdentification pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                ClientIdentificationReceived(this, new DataReceivedEventArgs<PDUClientIdentification>(pdu, _userData));
            }, null);
        }
        else
        {
            ClientIdentificationReceived(this, new DataReceivedEventArgs<PDUClientIdentification>(pdu, _userData));
        }
    }

    private void RaiseServerIdentificationReceived(PDUServerIdentification pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                ServerIdentificationReceived(this, new DataReceivedEventArgs<PDUServerIdentification>(pdu, _userData));
            }, null);
        }
        else
        {
            ServerIdentificationReceived(this, new DataReceivedEventArgs<PDUServerIdentification>(pdu, _userData));
        }
    }

    private void RaiseAddAtcReceived(PDUAddATC pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                AddAtcReceived(this, new DataReceivedEventArgs<PDUAddATC>(pdu, _userData));
            }, null);
        }
        else
        {
            AddAtcReceived(this, new DataReceivedEventArgs<PDUAddATC>(pdu, _userData));
        }
    }

    private void RaiseDeleteAtcReceived(PDUDeleteATC pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                DeleteAtcReceived(this, new DataReceivedEventArgs<PDUDeleteATC>(pdu, _userData));
            }, null);
        }
        else
        {
            DeleteAtcReceived(this, new DataReceivedEventArgs<PDUDeleteATC>(pdu, _userData));
        }
    }

    private void RaiseAddPilotReceived(PDUAddPilot pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                AddPilotReceived(this, new DataReceivedEventArgs<PDUAddPilot>(pdu, _userData));
            }, null);
        }
        else
        {
            AddPilotReceived(this, new DataReceivedEventArgs<PDUAddPilot>(pdu, _userData));
        }
    }

    private void RaiseDeletePilotReceived(PDUDeletePilot pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                DeletePilotReceived(this, new DataReceivedEventArgs<PDUDeletePilot>(pdu, _userData));
            }, null);
        }
        else
        {
            DeletePilotReceived(this, new DataReceivedEventArgs<PDUDeletePilot>(pdu, _userData));
        }
    }

    private void RaiseTextMessageReceived(PDUTextMessage pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                TextMessageReceived(this, new DataReceivedEventArgs<PDUTextMessage>(pdu, _userData));
            }, null);
        }
        else
        {
            TextMessageReceived(this, new DataReceivedEventArgs<PDUTextMessage>(pdu, _userData));
        }
    }

    private void RaiseAtcMessageReceived(PDUATCMessage pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                AtcMessageReceived(this, new DataReceivedEventArgs<PDUATCMessage>(pdu, _userData));
            }, null);
        }
        else
        {
            AtcMessageReceived(this, new DataReceivedEventArgs<PDUATCMessage>(pdu, _userData));
        }
    }

    private void RaiseRadioMessageReceived(PDURadioMessage pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                RadioMessageReceived(this, new DataReceivedEventArgs<PDURadioMessage>(pdu, _userData));
            }, null);
        }
        else
        {
            RadioMessageReceived(this, new DataReceivedEventArgs<PDURadioMessage>(pdu, _userData));
        }
    }

    private void RaiseBroadcastMessageReceived(PDUBroadcastMessage pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                BroadcastMessageReceived(this, new DataReceivedEventArgs<PDUBroadcastMessage>(pdu, _userData));
            }, null);
        }
        else
        {
            BroadcastMessageReceived(this, new DataReceivedEventArgs<PDUBroadcastMessage>(pdu, _userData));
        }
    }

    private void RaiseWallopReceived(PDUWallop pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                WallopReceived(this, new DataReceivedEventArgs<PDUWallop>(pdu, _userData));
            }, null);
        }
        else
        {
            WallopReceived(this, new DataReceivedEventArgs<PDUWallop>(pdu, _userData));
        }
    }

    private void RaiseWeatherProfileRequestReceived(PDUWeatherProfileRequest pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                WeatherProfileRequestReceived(this, new DataReceivedEventArgs<PDUWeatherProfileRequest>(pdu, _userData));
            }, null);
        }
        else
        {
            WeatherProfileRequestReceived(this, new DataReceivedEventArgs<PDUWeatherProfileRequest>(pdu, _userData));
        }
    }

    private void RaiseWindDataReceived(PDUWindData pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                WindDataReceived(this, new DataReceivedEventArgs<PDUWindData>(pdu, _userData));
            }, null);
        }
        else
        {
            WindDataReceived(this, new DataReceivedEventArgs<PDUWindData>(pdu, _userData));
        }
    }

    private void RaiseTemperatureDataReceived(PDUTemperatureData pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                TemperatureDataReceived(this, new DataReceivedEventArgs<PDUTemperatureData>(pdu, _userData));
            }, null);
        }
        else
        {
            TemperatureDataReceived(this, new DataReceivedEventArgs<PDUTemperatureData>(pdu, _userData));
        }
    }

    private void RaiseCloudDataReceived(PDUCloudData pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                CloudDataReceived(this, new DataReceivedEventArgs<PDUCloudData>(pdu, _userData));
            }, null);
        }
        else
        {
            CloudDataReceived(this, new DataReceivedEventArgs<PDUCloudData>(pdu, _userData));
        }
    }

    private void RaiseHandoffCancelledReceived(PDUHandoffCancelled pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                HandoffCancelledReceived(this, new DataReceivedEventArgs<PDUHandoffCancelled>(pdu, _userData));
            }, null);
        }
        else
        {
            HandoffCancelledReceived(this, new DataReceivedEventArgs<PDUHandoffCancelled>(pdu, _userData));
        }
    }

    private void RaiseFlightStripReceived(PDUFlightStrip pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                FlightStripReceived(this, new DataReceivedEventArgs<PDUFlightStrip>(pdu, _userData));
            }, null);
        }
        else
        {
            FlightStripReceived(this, new DataReceivedEventArgs<PDUFlightStrip>(pdu, _userData));
        }
    }

    private void RaisePushToDepartureListReceived(PDUPushToDepartureList pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                PushToDepartureListReceived(this, new DataReceivedEventArgs<PDUPushToDepartureList>(pdu, _userData));
            }, null);
        }
        else
        {
            PushToDepartureListReceived(this, new DataReceivedEventArgs<PDUPushToDepartureList>(pdu, _userData));
        }
    }

    private void RaisePointoutReceived(PDUPointout pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                PointoutReceived(this, new DataReceivedEventArgs<PDUPointout>(pdu, _userData));
            }, null);
        }
        else
        {
            PointoutReceived(this, new DataReceivedEventArgs<PDUPointout>(pdu, _userData));
        }
    }

    private void RaiseIHaveTargetReceived(PDUIHaveTarget pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                IHaveTargetReceived(this, new DataReceivedEventArgs<PDUIHaveTarget>(pdu, _userData));
            }, null);
        }
        else
        {
            IHaveTargetReceived(this, new DataReceivedEventArgs<PDUIHaveTarget>(pdu, _userData));
        }
    }

    private void RaiseSharedStateReceived(PDUSharedState pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                SharedStateReceived(this, new DataReceivedEventArgs<PDUSharedState>(pdu, _userData));
            }, null);
        }
        else
        {
            SharedStateReceived(this, new DataReceivedEventArgs<PDUSharedState>(pdu, _userData));
        }
    }

    private void RaiseLandLineCommandReceived(PDULandLineCommand pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                LandLineCommandReceived(this, new DataReceivedEventArgs<PDULandLineCommand>(pdu, _userData));
            }, null);
        }
        else
        {
            LandLineCommandReceived(this, new DataReceivedEventArgs<PDULandLineCommand>(pdu, _userData));
        }
    }

    private void RaisePlaneInfoRequestReceived(PDUPlaneInfoRequest pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                PlaneInfoRequestReceived(this, new DataReceivedEventArgs<PDUPlaneInfoRequest>(pdu, _userData));
            }, null);
        }
        else
        {
            PlaneInfoRequestReceived(this, new DataReceivedEventArgs<PDUPlaneInfoRequest>(pdu, _userData));
        }
    }

    private void RaisePlaneInfoResponseReceived(PDUPlaneInfoResponse pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                PlaneInfoResponseReceived(this, new DataReceivedEventArgs<PDUPlaneInfoResponse>(pdu, _userData));
            }, null);
        }
        else
        {
            PlaneInfoResponseReceived(this, new DataReceivedEventArgs<PDUPlaneInfoResponse>(pdu, _userData));
        }
    }

    private void RaiseLegacyPlaneInfoResponseReceived(PDULegacyPlaneInfoResponse pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                LegacyPlaneInfoResponseReceived(this, new DataReceivedEventArgs<PDULegacyPlaneInfoResponse>(pdu, _userData));
            }, null);
        }
        else
        {
            LegacyPlaneInfoResponseReceived(this, new DataReceivedEventArgs<PDULegacyPlaneInfoResponse>(pdu, _userData));
        }
    }

    private void RaiseFlightPlanReceived(PDUFlightPlan pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                FlightPlanReceived(this, new DataReceivedEventArgs<PDUFlightPlan>(pdu, _userData));
            }, null);
        }
        else
        {
            FlightPlanReceived(this, new DataReceivedEventArgs<PDUFlightPlan>(pdu, _userData));
        }
    }

    private void RaiseFlightPlanAmendmentReceived(PDUFlightPlanAmendment pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                FlightPlanAmendmentReceived(this, new DataReceivedEventArgs<PDUFlightPlanAmendment>(pdu, _userData));
            }, null);
        }
        else
        {
            FlightPlanAmendmentReceived(this, new DataReceivedEventArgs<PDUFlightPlanAmendment>(pdu, _userData));
        }
    }

    private void RaisePingReceived(PDUPing pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                PingReceived(this, new DataReceivedEventArgs<PDUPing>(pdu, _userData));
            }, null);
        }
        else
        {
            PingReceived(this, new DataReceivedEventArgs<PDUPing>(pdu, _userData));
        }
    }

    private void RaisePongReceived(PDUPong pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                PongReceived(this, new DataReceivedEventArgs<PDUPong>(pdu, _userData));
            }, null);
        }
        else
        {
            PongReceived(this, new DataReceivedEventArgs<PDUPong>(pdu, _userData));
        }
    }

    private void RaiseHandoffReceived(PDUHandoff pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                HandoffReceived(this, new DataReceivedEventArgs<PDUHandoff>(pdu, _userData));
            }, null);
        }
        else
        {
            HandoffReceived(this, new DataReceivedEventArgs<PDUHandoff>(pdu, _userData));
        }
    }

    private void RaiseHandoffAcceptReceived(PDUHandoffAccept pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                HandoffAcceptReceived(this, new DataReceivedEventArgs<PDUHandoffAccept>(pdu, _userData));
            }, null);
        }
        else
        {
            HandoffAcceptReceived(this, new DataReceivedEventArgs<PDUHandoffAccept>(pdu, _userData));
        }
    }

    private void RaiseAcarsQueryReceived(PDUMetarRequest pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                AcarsQueryReceived(this, new DataReceivedEventArgs<PDUMetarRequest>(pdu, _userData));
            }, null);
        }
        else
        {
            AcarsQueryReceived(this, new DataReceivedEventArgs<PDUMetarRequest>(pdu, _userData));
        }
    }

    private void RaiseAcarsResponseReceived(PDUMetarResponse pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                AcarsResponseReceived(this, new DataReceivedEventArgs<PDUMetarResponse>(pdu, _userData));
            }, null);
        }
        else
        {
            AcarsResponseReceived(this, new DataReceivedEventArgs<PDUMetarResponse>(pdu, _userData));
        }
    }

    private void RaiseClientQueryReceived(PDUClientQuery pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                ClientQueryReceived(this, new DataReceivedEventArgs<PDUClientQuery>(pdu, _userData));
            }, null);
        }
        else
        {
            ClientQueryReceived(this, new DataReceivedEventArgs<PDUClientQuery>(pdu, _userData));
        }
    }

    private void RaiseClientQueryResponseReceived(PDUClientQueryResponse pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                ClientQueryResponseReceived(this, new DataReceivedEventArgs<PDUClientQueryResponse>(pdu, _userData));
            }, null);
        }
        else
        {
            ClientQueryResponseReceived(this, new DataReceivedEventArgs<PDUClientQueryResponse>(pdu, _userData));
        }
    }

    private void RaiseAuthChallengeReceived(PDUAuthChallenge pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                AuthChallengeReceived(this, new DataReceivedEventArgs<PDUAuthChallenge>(pdu, _userData));
            }, null);
        }
        else
        {
            AuthChallengeReceived(this, new DataReceivedEventArgs<PDUAuthChallenge>(pdu, _userData));
        }
    }

    private void RaiseAuthResponseReceived(PDUAuthResponse pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                AuthResponseReceived(this, new DataReceivedEventArgs<PDUAuthResponse>(pdu, _userData));
            }, null);
        }
        else
        {
            AuthResponseReceived(this, new DataReceivedEventArgs<PDUAuthResponse>(pdu, _userData));
        }
    }

    private void RaiseKillRequestReceived(PDUKillRequest pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                KillRequestReceived(this, new DataReceivedEventArgs<PDUKillRequest>(pdu, _userData));
            }, null);
        }
        else
        {
            KillRequestReceived(this, new DataReceivedEventArgs<PDUKillRequest>(pdu, _userData));
        }
    }

    private void RaiseProtocolErrorReceived(PDUProtocolError pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                ProtocolErrorReceived(this, new DataReceivedEventArgs<PDUProtocolError>(pdu, _userData));
            }, null);
        }
        else
        {
            ProtocolErrorReceived(this, new DataReceivedEventArgs<PDUProtocolError>(pdu, _userData));
        }
    }

    private void RaiseVersionRequestReceived(PDUVersionRequest pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                VersionRequestReceived(this, new DataReceivedEventArgs<PDUVersionRequest>(pdu, _userData));
            }, null);
        }
        else
        {
            VersionRequestReceived(this, new DataReceivedEventArgs<PDUVersionRequest>(pdu, _userData));
        }
    }

    private void RaiseSendFastPositionsReceived(PDUSendFastPositions pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                SendFastPositionsReceived(this, new DataReceivedEventArgs<PDUSendFastPositions>(pdu, _userData));
            }, null);
        }
        else
        {
            SendFastPositionsReceived(this, new DataReceivedEventArgs<PDUSendFastPositions>(pdu, _userData));
        }
    }

    private void RaiseChangeServerReceived(PDUChangeServer pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                ChangeServerReceived(this, new DataReceivedEventArgs<PDUChangeServer>(pdu, _userData));
            }, null);
        }
        else
        {
            ChangeServerReceived(this, new DataReceivedEventArgs<PDUChangeServer>(pdu, _userData));
        }
    }

    private void RaiseMuteReceived(PDUMute pdu)
    {
        if (_syncContext != null)
        {
            _syncContext.Post((_) =>
            {
                MuteReceived(this, new DataReceivedEventArgs<PDUMute>(pdu, _userData));
            }, null);
        }
        else
        {
            MuteReceived(this, new DataReceivedEventArgs<PDUMute>(pdu, _userData));
        }
    }

    public void SetSyncContext(SynchronizationContext context)
    {
        _syncContext = context;
    }

    public void Connect(string address, int port, bool challengeServer = true)
    {
        _challengeServer = challengeServer;
        try
        {
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (Regex.IsMatch(address, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
            {
                BeginConnect(IPAddress.Parse(address), port);
                return;
            }
            Dns.BeginGetHostEntry(address, ResolveServerCallback, port);
        }
        catch (Exception se)
        {
            RaiseNetworkError($"Connection failed: {se.Message}");
            RaiseNetworkConnectionFailed();
        }

        _partialPacket = string.Empty;
    }

    private void BeginConnect(IPAddress ip, int port)
    {
        IPEndPoint ipEnd = new IPEndPoint(ip, port);
        _clientSocket?.BeginConnect(ipEnd, ConnectCallback, _clientSocket);
    }

    private void ResolveServerCallback(IAsyncResult ar)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(ar.AsyncState);
            IPHostEntry hostInfo = Dns.EndGetHostEntry(ar);
            IPAddress ip = (from a in hostInfo.AddressList
                where a.AddressFamily == AddressFamily.InterNetwork
                select a).First();
            BeginConnect(ip, (int)ar.AsyncState);
        }
        catch (Exception ex)
        {
            RaiseNetworkError($"Connection failed: {ex.Message}");
            RaiseNetworkConnectionFailed();
        }
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        ArgumentNullException.ThrowIfNull(ar.AsyncState);

        Socket sock = (Socket)ar.AsyncState;
        try
        {
            sock.EndConnect(ar);
            RaiseNetworkConnected();
            WaitForData();
        }
        catch (SocketException se)
        {
            RaiseNetworkError($"Connection failed: ({se.ErrorCode}) {se.Message}");
            RaiseNetworkConnectionFailed();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public void Disconnect()
    {
        ResetServerAuthSession();
        if (_clientSocket != null)
        {
            try
            {
                _clientSocket.Shutdown(SocketShutdown.Both);
                _clientSocket.Close();
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            _clientSocket = null;
            RaiseNetworkDisconnected();
        }
    }

    private void SendData(string data)
    {
        if (!Connected)
        {
            return;
        }
        try
        {
            var bytes = Encoding.Default.GetBytes(data);
            _clientSocket?.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, _clientSocket);

            RaiseRawDataSent(data);
        }
        catch (SocketException se)
        {
            if (se.ErrorCode == 10053 || se.ErrorCode == 10054)
            {
                Disconnect();
            }
            else
            {
                var err = $"Send failed: ({se.ErrorCode}) {se.Message}";
                RaiseNetworkError(err);
            }
        }
    }

    private void SendCallback(IAsyncResult iar)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(iar.AsyncState);
            Socket sock = (Socket)iar.AsyncState;
            sock.EndSend(iar);
        }
        catch (ObjectDisposedException) { } // OK to swallow these ... just means the socket was closed.
    }

    public void SendPdu(PDUBase pdu)
    {
        if (_challengeServer)
        {
            if (pdu is PDUClientIdentification identification && string.IsNullOrEmpty(identification.InitialChallenge))
            {
                var initialChallenge = _clientAuth.GenerateAuthChallenge();
                _serverAuthSessionKey = _clientAuth.GenerateAuthResponse(initialChallenge);
                identification.InitialChallenge = initialChallenge;
            }

            if (pdu is PDUAddPilot { ProtocolRevision: >= ProtocolRevision.VatsimAuth } or PDUAddATC
                {
                    ProtocolRevision: >= ProtocolRevision.VatsimAuth
                })
            {
                _currentCallsign = pdu.From;
                _serverAuthTimer = new Timer(CheckServerAuth);
                _serverAuthTimer.Change(ServerAuthChallengeResponseWindow, Timeout.Infinite);
            }
        }
        SendData(pdu.Serialize() + PDUBase.PACKET_DELIMITER);
    }

    private void CheckServerAuth(object? state)
    {
        // Check if this is the first auth check. If so, we generate the session key and send a challenge.
        if (string.IsNullOrEmpty(_serverAuthChallengeKey))
        {
            _serverAuthChallengeKey = _serverAuthSessionKey;
            ChallengeServer();
            return;
        }

        // Check if we have a pending auth challenge. If we do, then the server has failed to respond to
        // the challenge in time, so we disconnect.
        if (!string.IsNullOrEmpty(_lastServerAuthChallenge))
        {
            RaiseNetworkError("The server has failed to respond to the authentication challenge.");
            Disconnect();
        }

        // If none of the above, challenge the server.
        ChallengeServer();
    }

    private void ChallengeServer()
    {
        ArgumentNullException.ThrowIfNull(_currentCallsign);

        _lastServerAuthChallenge = _clientAuth.GenerateAuthChallenge();
        PDUAuthChallenge pdu = new PDUAuthChallenge(_currentCallsign, PDUBase.SERVER_CALLSIGN, _lastServerAuthChallenge);
        SendPdu(pdu);
        _serverAuthTimer?.Change(ServerAuthChallengeResponseWindow, Timeout.Infinite);
    }

    private void CheckServerAuthChallengeResponse(string response)
    {
        if (_serverAuthTimer == null)
        {
            return;
        }
        var expectedResponse = _clientAuth.GenerateAuthResponse(_lastServerAuthChallenge, _serverAuthChallengeKey);
        if (response != expectedResponse)
        {
            RaiseNetworkError("The server has failed to respond correctly to the authentication challenge.");
            Disconnect();
        }
        else
        {
            _lastServerAuthChallenge = string.Empty;
            _serverAuthChallengeKey = GenerateMd5Digest(_serverAuthSessionKey + response);
            _serverAuthTimer.Change(ServerAuthChallengeInterval, Timeout.Infinite);
        }
    }

    private void ResetServerAuthSession()
    {
        _serverAuthTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _serverAuthSessionKey = string.Empty;
        _serverAuthChallengeKey = string.Empty;
        _lastServerAuthChallenge = string.Empty;
    }

    private class SocketPacket
    {
        public Socket? ThisSocket;
        public readonly byte[] DataBuffer = new byte[1024];
    }

    private void WaitForData()
    {
        try
        {
            _incomingDataCallBack ??= OnDataReceived;
            if (_clientSocket == null)
            {
                return;
            }
            SocketPacket theSockPkt = new SocketPacket
            {
                ThisSocket = _clientSocket
            };
            _clientSocket.BeginReceive(
                theSockPkt.DataBuffer,
                0, theSockPkt.DataBuffer.Length,
                SocketFlags.None,
                _incomingDataCallBack,
                theSockPkt
            );
        }
        catch (SocketException se)
        {
            if (se.ErrorCode == 10053 || se.ErrorCode == 10054)
            {
                Disconnect();
            }
            else
            {
                var err = $"BeginReceive failed: ({se.ErrorCode}) {se.Message}";
                RaiseNetworkError(err);
            }
        }
    }

    private void OnDataReceived(IAsyncResult async)
    {
        try
        {
            if (async.AsyncState == null)
                return;

            SocketPacket theSockId = (SocketPacket)async.AsyncState;

            if (theSockId.ThisSocket == null)
                return;

            var bytesReceived = theSockId.ThisSocket.EndReceive(async);
            if (bytesReceived == 0)
            {
                Disconnect();
                return;
            }
            var chars = new char[bytesReceived + 1];
            Decoder d = Encoding.Default.GetDecoder();
            d.GetChars(theSockId.DataBuffer, 0, bytesReceived, chars, 0);
            string data = new(chars);
            ProcessData(data);
            WaitForData();
        }
        catch (ObjectDisposedException)
        {
            Disconnect();
        }
        catch (SocketException se)
        {
            if (se.ErrorCode != 995 && se.ErrorCode != 89)
            {
                var err = $"EndReceive failed: ({se.ErrorCode}) {se.Message}";
                RaiseNetworkError(err);
            }
            Disconnect();
        }
    }

    private void ProcessData(string data)
    {
        if (data.Length == 0)
        {
            return;
        }

        // Strip out trailing null, if any.
        if (data.Substring(data.Length - 1) == "\0")
        {
            data = data.Substring(0, data.Length - 1);
        }

        data = _partialPacket + data;
        _partialPacket = "";

        // Split the data into PDUs.
        var packets = data.Split([PDUBase.PACKET_DELIMITER], StringSplitOptions.None);

        // If the last packet has content, it's an incomplete packet.
        var topIndex = packets.Length - 1;
        if (packets[topIndex].Length > 0)
        {
            _partialPacket = packets[topIndex];
            packets[topIndex] = "";
        }

        // Process each packet.
        foreach (var packet in packets)
        {
            if (packet.Length == 0)
            {
                continue;
            }

            RaiseRawDataReceived(packet + PDUBase.PACKET_DELIMITER);
            try
            {
                var fields = packet.Split([PDUBase.DELIMITER], StringSplitOptions.None);
                var prefixChar = fields[0][0];
                switch (prefixChar)
                {
                    case '@':
                        fields[0] = fields[0].Substring(1);
                        RaisePilotPositionReceived(PDUPilotPosition.Parse(fields));
                        break;
                    case '^':
                        fields[0] = fields[0].Substring(1);
                        RaiseFastPilotPositionReceived(PDUFastPilotPosition.Parse(FastPilotPositionType.Fast, fields));
                        break;
                    case '%':
                        fields[0] = fields[0].Substring(1);
                        RaiseAtcPositionReceived(PDUATCPosition.Parse(fields));
                        break;
                    case '\'':
                        fields[0] = fields[0].Substring(1);
                        RaiseSecondaryVisCenterReceived(PDUSecondaryVisCenter.Parse(fields));
                        break;
                    case '#':
                    case '$':
                        if (fields[0].Length < 3)
                        {
                            throw new PDUFormatException("Invalid PDU type.", packet);
                        }
                        var pduTypeId = fields[0].Substring(0, 3);
                        fields[0] = fields[0].Substring(3);
                        switch (pduTypeId)
                        {
                            case "$DI":
                                {
                                    PDUServerIdentification pdu = PDUServerIdentification.Parse(fields);
                                    if (_clientAuth.ClientId != 0)
                                    {
                                        _clientAuthSessionKey = _clientAuth.GenerateAuthResponse(pdu.InitialChallengeKey);
                                        _clientAuthChallengeKey = _clientAuthSessionKey;
                                    }
                                    RaiseServerIdentificationReceived(pdu);
                                    break;
                                }
                            case "$ID":
                                RaiseClientIdentificationReceived(PDUClientIdentification.Parse(fields));
                                break;
                            case "#AA":
                                RaiseAddAtcReceived(PDUAddATC.Parse(fields));
                                break;
                            case "#DA":
                                RaiseDeleteAtcReceived(PDUDeleteATC.Parse(fields));
                                break;
                            case "#AP":
                                RaiseAddPilotReceived(PDUAddPilot.Parse(fields));
                                break;
                            case "#DP":
                                RaiseDeletePilotReceived(PDUDeletePilot.Parse(fields));
                                break;
                            case "#TM":
                                ProcessTextMessage(fields);
                                break;
                            case "#WX":
                                RaiseWeatherProfileRequestReceived(PDUWeatherProfileRequest.Parse(fields));
                                break;
                            case "#WD":
                                RaiseWindDataReceived(PDUWindData.Parse(fields));
                                break;
                            case "#DL":
                                // Specs dictate that this packet should be disregarded.
                                break;
                            case "#TD":
                                RaiseTemperatureDataReceived(PDUTemperatureData.Parse(fields));
                                break;
                            case "#CD":
                                RaiseCloudDataReceived(PDUCloudData.Parse(fields));
                                break;
                            case "#PC":
                                if (fields.Length < 4)
                                {
                                    if (!IgnoreUnknownPackets)
                                    {
                                        throw new PDUFormatException("Too few fields in #PC packet.", packet);
                                    }
                                }
                                else if (fields[2] != "CCP")
                                {
                                    if (!IgnoreUnknownPackets)
                                    {
                                        throw new PDUFormatException("Unknown #PC packet type.", packet);
                                    }
                                }
                                else
                                {
                                    switch (fields[3])
                                    {
                                        case "VER":
                                            RaiseVersionRequestReceived(PDUVersionRequest.Parse(fields));
                                            break;
                                        case "ID":
                                        case "DI":
                                            // These subtypes are deprecated. Ignore.
                                            break;
                                        case "HC":
                                            RaiseHandoffCancelledReceived(PDUHandoffCancelled.Parse(fields));
                                            break;
                                        case "ST":
                                            RaiseFlightStripReceived(PDUFlightStrip.Parse(fields));
                                            break;
                                        case "DP":
                                            RaisePushToDepartureListReceived(PDUPushToDepartureList.Parse(fields));
                                            break;
                                        case "PT":
                                            RaisePointoutReceived(PDUPointout.Parse(fields));
                                            break;
                                        case "IH":
                                            RaiseIHaveTargetReceived(PDUIHaveTarget.Parse(fields));
                                            break;
                                        case "SC":
                                        case "BC":
                                        case "VT":
                                        case "TA":
                                        case "GD":
                                            RaiseSharedStateReceived(PDUSharedState.Parse(fields));
                                            break;
                                        case "IC":
                                        case "IK":
                                        case "IB":
                                        case "EC":
                                        case "OV":
                                        case "OK":
                                        case "OB":
                                        case "EO":
                                        case "MN":
                                        case "MK":
                                        case "MB":
                                        case "EM":
                                            RaiseLandLineCommandReceived(PDULandLineCommand.Parse(fields));
                                            break;
                                        default:
                                            if (!IgnoreUnknownPackets)
                                            {
                                                throw new PDUFormatException("Unknown #PC packet subtype.", packet);
                                            }

                                            break;
                                    }
                                }
                                break;
                            case "#SB":
                                if (fields.Length < 3)
                                {
                                    if (!IgnoreUnknownPackets)
                                    {
                                        throw new PDUFormatException("Too few fields in #SB packet.", packet);
                                    }
                                }
                                else
                                {
                                    switch (fields[2])
                                    {
                                        case "PIR":
                                            RaisePlaneInfoRequestReceived(PDUPlaneInfoRequest.Parse(fields));
                                            break;
                                        case "PI":
                                            if (fields.Length < 4)
                                            {
                                                if (!IgnoreUnknownPackets)
                                                {
                                                    throw new PDUFormatException("Too few fields in #SB packet.", packet);
                                                }
                                            }
                                            else
                                            {
                                                switch (fields[3])
                                                {
                                                    case "X":
                                                        RaiseLegacyPlaneInfoResponseReceived(PDULegacyPlaneInfoResponse.Parse(fields));
                                                        break;
                                                    case "GEN":
                                                        RaisePlaneInfoResponseReceived(PDUPlaneInfoResponse.Parse(fields));
                                                        break;
                                                    default:
                                                        if (!IgnoreUnknownPackets)
                                                        {
                                                            throw new PDUFormatException("Unknown #SB packet subtype.", packet);
                                                        }

                                                        break;
                                                }
                                            }
                                            break;
                                        default:
                                            if (!IgnoreUnknownPackets)
                                            {
                                                throw new PDUFormatException("Unknown #SB packet type.", packet);
                                            }
                                            break;
                                    }
                                }
                                break;
                            case "$FP":
                                try
                                {
                                    RaiseFlightPlanReceived(PDUFlightPlan.Parse(fields));
                                }
                                catch (PDUFormatException) { } // Sometimes the server will send a malformed $FP. Ignore it.
                                break;
                            case "$AM":
                                RaiseFlightPlanAmendmentReceived(PDUFlightPlanAmendment.Parse(fields));
                                break;
                            case "$PI":
                                RaisePingReceived(PDUPing.Parse(fields));
                                break;
                            case "$PO":
                                RaisePongReceived(PDUPong.Parse(fields));
                                break;
                            case "$HO":
                                RaiseHandoffReceived(PDUHandoff.Parse(fields));
                                break;
                            case "$HA":
                                RaiseHandoffAcceptReceived(PDUHandoffAccept.Parse(fields));
                                break;
                            case "$AX":
                                RaiseAcarsQueryReceived(PDUMetarRequest.Parse(fields));
                                break;
                            case "$AR":
                                RaiseAcarsResponseReceived(PDUMetarResponse.Parse(fields));
                                break;
                            case "$CQ":
                                RaiseClientQueryReceived(PDUClientQuery.Parse(fields));
                                break;
                            case "$CR":
                                RaiseClientQueryResponseReceived(PDUClientQueryResponse.Parse(fields));
                                break;
                            case "$ZC":
                                if (_clientAuth.ClientId != 0)
                                {
                                    PDUAuthChallenge pdu = PDUAuthChallenge.Parse(fields);
                                    var response = _clientAuth.GenerateAuthResponse(pdu.Challenge, _clientAuthChallengeKey);
                                    _clientAuthChallengeKey = GenerateMd5Digest(_clientAuthSessionKey + response);
                                    PDUAuthResponse responsePdu = new PDUAuthResponse(pdu.To, pdu.From, response);
                                    SendPdu(responsePdu);
                                }
                                else
                                {
                                    RaiseAuthChallengeReceived(PDUAuthChallenge.Parse(fields));
                                }
                                break;
                            case "$ZR":
                                {
                                    PDUAuthResponse pdu = PDUAuthResponse.Parse(fields);
                                    if (_challengeServer && _clientAuth.ClientId != 0 && !string.IsNullOrEmpty(_serverAuthChallengeKey) && !string.IsNullOrEmpty(_lastServerAuthChallenge))
                                    {
                                        CheckServerAuthChallengeResponse(pdu.Response);
                                    }
                                    else
                                    {
                                        RaiseAuthResponseReceived(pdu);
                                    }
                                    break;
                                }
                            case "$!!":
                                RaiseKillRequestReceived(PDUKillRequest.Parse(fields));
                                break;
                            case "$ER":
                                RaiseProtocolErrorReceived(PDUProtocolError.Parse(fields));
                                break;
                            case "$SF":
                                RaiseSendFastPositionsReceived(PDUSendFastPositions.Parse(fields));
                                break;
                            case "#SL":
                                RaiseFastPilotPositionReceived(PDUFastPilotPosition.Parse(FastPilotPositionType.Slow, fields));
                                break;
                            case "#ST":
                                RaiseFastPilotPositionReceived(PDUFastPilotPosition.Parse(FastPilotPositionType.Stopped, fields));
                                break;
                            case "$XX":
                                RaiseChangeServerReceived(PDUChangeServer.Parse(fields));
                                break;
                            case "#MU":
                                RaiseMuteReceived(PDUMute.Parse(fields));
                                break;
                            default:
                                if (!IgnoreUnknownPackets)
                                {
                                    throw new PDUFormatException("Unknown PDU type: " + pduTypeId, packet);
                                }
                                break;
                        }
                        break;
                    default:
                        if (!IgnoreUnknownPackets)
                        {
                            throw new PDUFormatException("Unknown PDU prefix: " + prefixChar, packet);
                        }
                        break;
                }
            }
            catch (PDUFormatException ex)
            {
                RaiseNetworkError($"{ex.Message} (Raw packet: {ex.RawMessage})");
            }
        }
    }

    private void ProcessTextMessage(string[] fields)
    {
        if (fields.Length < 3)
        {
            throw new PDUFormatException("Invalid field count.", PDUBase.Reassemble(fields));
        }

        // #TMs are allowed to have colons in the message field, so here we need
        // to rejoin the fields then re-split with a limit of 3 substrings.
        var raw = PDUBase.Reassemble(fields);
        fields = raw.Split([PDUBase.DELIMITER], 3);

        // Check for special case recipients.
        switch (fields[1])
        {
            case "*":
                RaiseBroadcastMessageReceived(PDUBroadcastMessage.Parse(fields));
                break;
            case "*S":
                RaiseWallopReceived(PDUWallop.Parse(fields));
                break;
            case "@49999":
                RaiseAtcMessageReceived(PDUATCMessage.Parse(fields));
                break;
            default:
                if (fields[1].Substring(0, 1) == "@")
                {
                    RaiseRadioMessageReceived(PDURadioMessage.Parse(fields));
                }
                else
                {
                    RaiseTextMessageReceived(PDUTextMessage.Parse(fields));
                }

                break;
        }
    }

    private static string GenerateMd5Digest(string value)
    {
        var data = Encoding.ASCII.GetBytes(value);
        var result = MD5.HashData(data);
        StringBuilder sb = new StringBuilder();
        foreach (var t in result)
        {
            sb.Append(t.ToString("x2"));
        }
        return sb.ToString();
    }
}
