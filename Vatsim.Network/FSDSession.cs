using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Vatsim.Network.PDU;

namespace Vatsim.Network
{
    public class FsdSession
    {
        private const int SERVER_AUTH_CHALLENGE_INTERVAL = 60000;
        private const int SERVER_AUTH_CHALLENGE_RESPONSE_WINDOW = 30000;

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

        private Socket? mClientSocket;
        private AsyncCallback? mIncomingDataCallBack;
        private string mPartialPacket = "";
        private string mClientAuthSessionKey = "";
        private string mClientAuthChallengeKey = "";
        private readonly object? mUserData;
        private SynchronizationContext? mSyncContext;
        private bool mChallengeServer;
        private string mServerAuthSessionKey = string.Empty;
        private string mServerAuthChallengeKey = string.Empty;
        private string mLastServerAuthChallenge = string.Empty;
        private Timer? mServerAuthTimer;
        private string? mCurrentCallsign;
        private readonly IClientAuth mClientAuth;
        
        public bool Connected => mClientSocket?.Connected ?? false;
        public bool IgnoreUnknownPackets { get; init; }
        public ClientProperties ClientProperties { get; set; }

        private FsdSession(ClientProperties properties, object? userData, SynchronizationContext? syncContext)
        {
            ClientProperties = properties;
            mUserData = userData;
            mSyncContext = syncContext;
            mClientAuth = new ClientAuth();
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
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    NetworkConnected(this, new NetworkEventArgs(mUserData));
                }, null);
            }
            else
            {
                NetworkConnected(this, new NetworkEventArgs(mUserData));
            }
        }

        private void RaiseNetworkDisconnected()
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    NetworkDisconnected(this, new NetworkEventArgs(mUserData));
                }, null);
            }
            else
            {
                NetworkDisconnected(this, new NetworkEventArgs(mUserData));
            }
        }

        private void RaiseNetworkConnectionFailed()
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    NetworkConnectionFailed(this, new NetworkEventArgs(mUserData));
                }, null);
            }
            else
            {
                NetworkConnectionFailed(this, new NetworkEventArgs(mUserData));
            }
        }

        private void RaiseNetworkError(string message)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    NetworkError(this, new NetworkErrorEventArgs(message, mUserData));
                }, null);
            }
            else
            {
                NetworkError(this, new NetworkErrorEventArgs(message, mUserData));
            }
        }

        private void RaiseRawDataSent(string data)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    RawDataSent(this, new RawDataEventArgs(data, mUserData));
                }, null);
            }
            else
            {
                RawDataSent(this, new RawDataEventArgs(data, mUserData));
            }
        }

        private void RaiseRawDataReceived(string data)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    RawDataReceived(this, new RawDataEventArgs(data, mUserData));
                }, null);
            }
            else
            {
                RawDataReceived(this, new RawDataEventArgs(data, mUserData));
            }
        }

        private void RaiseAtcPositionReceived(PDUATCPosition pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    AtcPositionReceived(this, new DataReceivedEventArgs<PDUATCPosition>(pdu, mUserData));
                }, null);
            }
            else
            {
                AtcPositionReceived(this, new DataReceivedEventArgs<PDUATCPosition>(pdu, mUserData));
            }
        }

        private void RaisePilotPositionReceived(PDUPilotPosition pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    PilotPositionReceived(this, new DataReceivedEventArgs<PDUPilotPosition>(pdu, mUserData));
                }, null);
            }
            else
            {
                PilotPositionReceived(this, new DataReceivedEventArgs<PDUPilotPosition>(pdu, mUserData));
            }
        }

        private void RaiseFastPilotPositionReceived(PDUFastPilotPosition pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    FastPilotPositionReceived(this, new DataReceivedEventArgs<PDUFastPilotPosition>(pdu, mUserData));
                }, null);
            }
            else
            {
                FastPilotPositionReceived(this, new DataReceivedEventArgs<PDUFastPilotPosition>(pdu, mUserData));
            }
        }

        private void RaiseSecondaryVisCenterReceived(PDUSecondaryVisCenter pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    SecondaryVisCenterReceived(this, new DataReceivedEventArgs<PDUSecondaryVisCenter>(pdu, mUserData));
                }, null);
            }
            else
            {
                SecondaryVisCenterReceived(this, new DataReceivedEventArgs<PDUSecondaryVisCenter>(pdu, mUserData));
            }
        }

        private void RaiseClientIdentificationReceived(PDUClientIdentification pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    ClientIdentificationReceived(this, new DataReceivedEventArgs<PDUClientIdentification>(pdu, mUserData));
                }, null);
            }
            else
            {
                ClientIdentificationReceived(this, new DataReceivedEventArgs<PDUClientIdentification>(pdu, mUserData));
            }
        }

        private void RaiseServerIdentificationReceived(PDUServerIdentification pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    ServerIdentificationReceived(this, new DataReceivedEventArgs<PDUServerIdentification>(pdu, mUserData));
                }, null);
            }
            else
            {
                ServerIdentificationReceived(this, new DataReceivedEventArgs<PDUServerIdentification>(pdu, mUserData));
            }
        }

        private void RaiseAddAtcReceived(PDUAddATC pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    AddAtcReceived(this, new DataReceivedEventArgs<PDUAddATC>(pdu, mUserData));
                }, null);
            }
            else
            {
                AddAtcReceived(this, new DataReceivedEventArgs<PDUAddATC>(pdu, mUserData));
            }
        }

        private void RaiseDeleteAtcReceived(PDUDeleteATC pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    DeleteAtcReceived(this, new DataReceivedEventArgs<PDUDeleteATC>(pdu, mUserData));
                }, null);
            }
            else
            {
                DeleteAtcReceived(this, new DataReceivedEventArgs<PDUDeleteATC>(pdu, mUserData));
            }
        }

        private void RaiseAddPilotReceived(PDUAddPilot pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    AddPilotReceived(this, new DataReceivedEventArgs<PDUAddPilot>(pdu, mUserData));
                }, null);
            }
            else
            {
                AddPilotReceived(this, new DataReceivedEventArgs<PDUAddPilot>(pdu, mUserData));
            }
        }

        private void RaiseDeletePilotReceived(PDUDeletePilot pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    DeletePilotReceived(this, new DataReceivedEventArgs<PDUDeletePilot>(pdu, mUserData));
                }, null);
            }
            else
            {
                DeletePilotReceived(this, new DataReceivedEventArgs<PDUDeletePilot>(pdu, mUserData));
            }
        }

        private void RaiseTextMessageReceived(PDUTextMessage pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    TextMessageReceived(this, new DataReceivedEventArgs<PDUTextMessage>(pdu, mUserData));
                }, null);
            }
            else
            {
                TextMessageReceived(this, new DataReceivedEventArgs<PDUTextMessage>(pdu, mUserData));
            }
        }

        private void RaiseAtcMessageReceived(PDUATCMessage pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    AtcMessageReceived(this, new DataReceivedEventArgs<PDUATCMessage>(pdu, mUserData));
                }, null);
            }
            else
            {
                AtcMessageReceived(this, new DataReceivedEventArgs<PDUATCMessage>(pdu, mUserData));
            }
        }

        private void RaiseRadioMessageReceived(PDURadioMessage pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    RadioMessageReceived(this, new DataReceivedEventArgs<PDURadioMessage>(pdu, mUserData));
                }, null);
            }
            else
            {
                RadioMessageReceived(this, new DataReceivedEventArgs<PDURadioMessage>(pdu, mUserData));
            }
        }

        private void RaiseBroadcastMessageReceived(PDUBroadcastMessage pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    BroadcastMessageReceived(this, new DataReceivedEventArgs<PDUBroadcastMessage>(pdu, mUserData));
                }, null);
            }
            else
            {
                BroadcastMessageReceived(this, new DataReceivedEventArgs<PDUBroadcastMessage>(pdu, mUserData));
            }
        }

        private void RaiseWallopReceived(PDUWallop pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    WallopReceived(this, new DataReceivedEventArgs<PDUWallop>(pdu, mUserData));
                }, null);
            }
            else
            {
                WallopReceived(this, new DataReceivedEventArgs<PDUWallop>(pdu, mUserData));
            }
        }

        private void RaiseWeatherProfileRequestReceived(PDUWeatherProfileRequest pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    WeatherProfileRequestReceived(this, new DataReceivedEventArgs<PDUWeatherProfileRequest>(pdu, mUserData));
                }, null);
            }
            else
            {
                WeatherProfileRequestReceived(this, new DataReceivedEventArgs<PDUWeatherProfileRequest>(pdu, mUserData));
            }
        }

        private void RaiseWindDataReceived(PDUWindData pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    WindDataReceived(this, new DataReceivedEventArgs<PDUWindData>(pdu, mUserData));
                }, null);
            }
            else
            {
                WindDataReceived(this, new DataReceivedEventArgs<PDUWindData>(pdu, mUserData));
            }
        }

        private void RaiseTemperatureDataReceived(PDUTemperatureData pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    TemperatureDataReceived(this, new DataReceivedEventArgs<PDUTemperatureData>(pdu, mUserData));
                }, null);
            }
            else
            {
                TemperatureDataReceived(this, new DataReceivedEventArgs<PDUTemperatureData>(pdu, mUserData));
            }
        }

        private void RaiseCloudDataReceived(PDUCloudData pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    CloudDataReceived(this, new DataReceivedEventArgs<PDUCloudData>(pdu, mUserData));
                }, null);
            }
            else
            {
                CloudDataReceived(this, new DataReceivedEventArgs<PDUCloudData>(pdu, mUserData));
            }
        }

        private void RaiseHandoffCancelledReceived(PDUHandoffCancelled pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    HandoffCancelledReceived(this, new DataReceivedEventArgs<PDUHandoffCancelled>(pdu, mUserData));
                }, null);
            }
            else
            {
                HandoffCancelledReceived(this, new DataReceivedEventArgs<PDUHandoffCancelled>(pdu, mUserData));
            }
        }

        private void RaiseFlightStripReceived(PDUFlightStrip pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    FlightStripReceived(this, new DataReceivedEventArgs<PDUFlightStrip>(pdu, mUserData));
                }, null);
            }
            else
            {
                FlightStripReceived(this, new DataReceivedEventArgs<PDUFlightStrip>(pdu, mUserData));
            }
        }

        private void RaisePushToDepartureListReceived(PDUPushToDepartureList pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    PushToDepartureListReceived(this, new DataReceivedEventArgs<PDUPushToDepartureList>(pdu, mUserData));
                }, null);
            }
            else
            {
                PushToDepartureListReceived(this, new DataReceivedEventArgs<PDUPushToDepartureList>(pdu, mUserData));
            }
        }

        private void RaisePointoutReceived(PDUPointout pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    PointoutReceived(this, new DataReceivedEventArgs<PDUPointout>(pdu, mUserData));
                }, null);
            }
            else
            {
                PointoutReceived(this, new DataReceivedEventArgs<PDUPointout>(pdu, mUserData));
            }
        }

        private void RaiseIHaveTargetReceived(PDUIHaveTarget pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    IHaveTargetReceived(this, new DataReceivedEventArgs<PDUIHaveTarget>(pdu, mUserData));
                }, null);
            }
            else
            {
                IHaveTargetReceived(this, new DataReceivedEventArgs<PDUIHaveTarget>(pdu, mUserData));
            }
        }

        private void RaiseSharedStateReceived(PDUSharedState pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    SharedStateReceived(this, new DataReceivedEventArgs<PDUSharedState>(pdu, mUserData));
                }, null);
            }
            else
            {
                SharedStateReceived(this, new DataReceivedEventArgs<PDUSharedState>(pdu, mUserData));
            }
        }

        private void RaiseLandLineCommandReceived(PDULandLineCommand pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    LandLineCommandReceived(this, new DataReceivedEventArgs<PDULandLineCommand>(pdu, mUserData));
                }, null);
            }
            else
            {
                LandLineCommandReceived(this, new DataReceivedEventArgs<PDULandLineCommand>(pdu, mUserData));
            }
        }

        private void RaisePlaneInfoRequestReceived(PDUPlaneInfoRequest pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    PlaneInfoRequestReceived(this, new DataReceivedEventArgs<PDUPlaneInfoRequest>(pdu, mUserData));
                }, null);
            }
            else
            {
                PlaneInfoRequestReceived(this, new DataReceivedEventArgs<PDUPlaneInfoRequest>(pdu, mUserData));
            }
        }

        private void RaisePlaneInfoResponseReceived(PDUPlaneInfoResponse pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    PlaneInfoResponseReceived(this, new DataReceivedEventArgs<PDUPlaneInfoResponse>(pdu, mUserData));
                }, null);
            }
            else
            {
                PlaneInfoResponseReceived(this, new DataReceivedEventArgs<PDUPlaneInfoResponse>(pdu, mUserData));
            }
        }

        private void RaiseLegacyPlaneInfoResponseReceived(PDULegacyPlaneInfoResponse pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    LegacyPlaneInfoResponseReceived(this, new DataReceivedEventArgs<PDULegacyPlaneInfoResponse>(pdu, mUserData));
                }, null);
            }
            else
            {
                LegacyPlaneInfoResponseReceived(this, new DataReceivedEventArgs<PDULegacyPlaneInfoResponse>(pdu, mUserData));
            }
        }

        private void RaiseFlightPlanReceived(PDUFlightPlan pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    FlightPlanReceived(this, new DataReceivedEventArgs<PDUFlightPlan>(pdu, mUserData));
                }, null);
            }
            else
            {
                FlightPlanReceived(this, new DataReceivedEventArgs<PDUFlightPlan>(pdu, mUserData));
            }
        }

        private void RaiseFlightPlanAmendmentReceived(PDUFlightPlanAmendment pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    FlightPlanAmendmentReceived(this, new DataReceivedEventArgs<PDUFlightPlanAmendment>(pdu, mUserData));
                }, null);
            }
            else
            {
                FlightPlanAmendmentReceived(this, new DataReceivedEventArgs<PDUFlightPlanAmendment>(pdu, mUserData));
            }
        }

        private void RaisePingReceived(PDUPing pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    PingReceived(this, new DataReceivedEventArgs<PDUPing>(pdu, mUserData));
                }, null);
            }
            else
            {
                PingReceived(this, new DataReceivedEventArgs<PDUPing>(pdu, mUserData));
            }
        }

        private void RaisePongReceived(PDUPong pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    PongReceived(this, new DataReceivedEventArgs<PDUPong>(pdu, mUserData));
                }, null);
            }
            else
            {
                PongReceived(this, new DataReceivedEventArgs<PDUPong>(pdu, mUserData));
            }
        }

        private void RaiseHandoffReceived(PDUHandoff pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    HandoffReceived(this, new DataReceivedEventArgs<PDUHandoff>(pdu, mUserData));
                }, null);
            }
            else
            {
                HandoffReceived(this, new DataReceivedEventArgs<PDUHandoff>(pdu, mUserData));
            }
        }

        private void RaiseHandoffAcceptReceived(PDUHandoffAccept pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    HandoffAcceptReceived(this, new DataReceivedEventArgs<PDUHandoffAccept>(pdu, mUserData));
                }, null);
            }
            else
            {
                HandoffAcceptReceived(this, new DataReceivedEventArgs<PDUHandoffAccept>(pdu, mUserData));
            }
        }

        private void RaiseAcarsQueryReceived(PDUMetarRequest pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    AcarsQueryReceived(this, new DataReceivedEventArgs<PDUMetarRequest>(pdu, mUserData));
                }, null);
            }
            else
            {
                AcarsQueryReceived(this, new DataReceivedEventArgs<PDUMetarRequest>(pdu, mUserData));
            }
        }

        private void RaiseAcarsResponseReceived(PDUMetarResponse pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    AcarsResponseReceived(this, new DataReceivedEventArgs<PDUMetarResponse>(pdu, mUserData));
                }, null);
            }
            else
            {
                AcarsResponseReceived(this, new DataReceivedEventArgs<PDUMetarResponse>(pdu, mUserData));
            }
        }

        private void RaiseClientQueryReceived(PDUClientQuery pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    ClientQueryReceived(this, new DataReceivedEventArgs<PDUClientQuery>(pdu, mUserData));
                }, null);
            }
            else
            {
                ClientQueryReceived(this, new DataReceivedEventArgs<PDUClientQuery>(pdu, mUserData));
            }
        }

        private void RaiseClientQueryResponseReceived(PDUClientQueryResponse pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    ClientQueryResponseReceived(this, new DataReceivedEventArgs<PDUClientQueryResponse>(pdu, mUserData));
                }, null);
            }
            else
            {
                ClientQueryResponseReceived(this, new DataReceivedEventArgs<PDUClientQueryResponse>(pdu, mUserData));
            }
        }

        private void RaiseAuthChallengeReceived(PDUAuthChallenge pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    AuthChallengeReceived(this, new DataReceivedEventArgs<PDUAuthChallenge>(pdu, mUserData));
                }, null);
            }
            else
            {
                AuthChallengeReceived(this, new DataReceivedEventArgs<PDUAuthChallenge>(pdu, mUserData));
            }
        }

        private void RaiseAuthResponseReceived(PDUAuthResponse pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    AuthResponseReceived(this, new DataReceivedEventArgs<PDUAuthResponse>(pdu, mUserData));
                }, null);
            }
            else
            {
                AuthResponseReceived(this, new DataReceivedEventArgs<PDUAuthResponse>(pdu, mUserData));
            }
        }

        private void RaiseKillRequestReceived(PDUKillRequest pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    KillRequestReceived(this, new DataReceivedEventArgs<PDUKillRequest>(pdu, mUserData));
                }, null);
            }
            else
            {
                KillRequestReceived(this, new DataReceivedEventArgs<PDUKillRequest>(pdu, mUserData));
            }
        }

        private void RaiseProtocolErrorReceived(PDUProtocolError pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    ProtocolErrorReceived(this, new DataReceivedEventArgs<PDUProtocolError>(pdu, mUserData));
                }, null);
            }
            else
            {
                ProtocolErrorReceived(this, new DataReceivedEventArgs<PDUProtocolError>(pdu, mUserData));
            }
        }

        private void RaiseVersionRequestReceived(PDUVersionRequest pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    VersionRequestReceived(this, new DataReceivedEventArgs<PDUVersionRequest>(pdu, mUserData));
                }, null);
            }
            else
            {
                VersionRequestReceived(this, new DataReceivedEventArgs<PDUVersionRequest>(pdu, mUserData));
            }
        }

        private void RaiseSendFastPositionsReceived(PDUSendFastPositions pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    SendFastPositionsReceived(this, new DataReceivedEventArgs<PDUSendFastPositions>(pdu, mUserData));
                }, null);
            }
            else
            {
                SendFastPositionsReceived(this, new DataReceivedEventArgs<PDUSendFastPositions>(pdu, mUserData));
            }
        }

        private void RaiseChangeServerReceived(PDUChangeServer pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    ChangeServerReceived(this, new DataReceivedEventArgs<PDUChangeServer>(pdu, mUserData));
                }, null);
            }
            else
            {
                ChangeServerReceived(this, new DataReceivedEventArgs<PDUChangeServer>(pdu, mUserData));
            }
        }

        private void RaiseMuteReceived(PDUMute pdu)
        {
            if (mSyncContext != null)
            {
                mSyncContext.Post((_) =>
                {
                    MuteReceived(this, new DataReceivedEventArgs<PDUMute>(pdu, mUserData));
                }, null);
            }
            else
            {
                MuteReceived(this, new DataReceivedEventArgs<PDUMute>(pdu, mUserData));
            }
        }

        public void SetSyncContext(SynchronizationContext context)
        {
            mSyncContext = context;
        }

        public void Connect(string address, int port, bool challengeServer = true)
        {
            mChallengeServer = challengeServer;
            try
            {
                mClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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

            mPartialPacket = string.Empty;
        }

        private void BeginConnect(IPAddress ip, int port)
        {
            IPEndPoint ipEnd = new IPEndPoint(ip, port);
            mClientSocket?.BeginConnect(ipEnd, ConnectCallback, mClientSocket);
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
            if (mClientSocket != null)
            {
                try
                {
                    mClientSocket.Shutdown(SocketShutdown.Both);
                    mClientSocket.Close();
                }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }
                mClientSocket = null;
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
                byte[] bytes = Encoding.Default.GetBytes(data);
                mClientSocket?.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, mClientSocket);

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
                    string err = $"Send failed: ({se.ErrorCode}) {se.Message}";
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
            if (mChallengeServer)
            {
                if (pdu is PDUClientIdentification identification && string.IsNullOrEmpty(identification.InitialChallenge))
                {
                    string initialChallenge = mClientAuth.GenerateAuthChallenge();
                    mServerAuthSessionKey = mClientAuth.GenerateAuthResponse(initialChallenge);
                    identification.InitialChallenge = initialChallenge;
                }

                if (pdu is PDUAddPilot { ProtocolRevision: >= ProtocolRevision.VatsimAuth } or PDUAddATC
                    {
                        ProtocolRevision: >= ProtocolRevision.VatsimAuth
                    })
                {
                    mCurrentCallsign = pdu.From;
                    mServerAuthTimer = new Timer(CheckServerAuth);
                    mServerAuthTimer.Change(SERVER_AUTH_CHALLENGE_RESPONSE_WINDOW, Timeout.Infinite);
                }
            }
            SendData(pdu.Serialize() + PDUBase.PACKET_DELIMITER);
        }

        private void CheckServerAuth(object? state)
        {
            // Check if this is the first auth check. If so, we generate the session key and send a challenge.
            if (string.IsNullOrEmpty(mServerAuthChallengeKey))
            {
                mServerAuthChallengeKey = mServerAuthSessionKey;
                ChallengeServer();
                return;
            }

            // Check if we have a pending auth challenge. If we do, then the server has failed to respond to
            // the challenge in time, so we disconnect.
            if (!string.IsNullOrEmpty(mLastServerAuthChallenge))
            {
                RaiseNetworkError("The server has failed to respond to the authentication challenge.");
                Disconnect();
            }

            // If none of the above, challenge the server.
            ChallengeServer();
        }

        private void ChallengeServer()
        {
            ArgumentNullException.ThrowIfNull(mCurrentCallsign);

            mLastServerAuthChallenge = mClientAuth.GenerateAuthChallenge();
            PDUAuthChallenge pdu = new PDUAuthChallenge(mCurrentCallsign, PDUBase.SERVER_CALLSIGN, mLastServerAuthChallenge);
            SendPdu(pdu);
            mServerAuthTimer?.Change(SERVER_AUTH_CHALLENGE_RESPONSE_WINDOW, Timeout.Infinite);
        }

        private void CheckServerAuthChallengeResponse(string response)
        {
            if (mServerAuthTimer == null)
            {
                return;
            }
            string expectedResponse = mClientAuth.GenerateAuthResponse(mLastServerAuthChallenge, mServerAuthChallengeKey);
            if (response != expectedResponse)
            {
                RaiseNetworkError("The server has failed to respond correctly to the authentication challenge.");
                Disconnect();
            }
            else
            {
                mLastServerAuthChallenge = string.Empty;
                mServerAuthChallengeKey = GenerateMd5Digest(mServerAuthSessionKey + response);
                mServerAuthTimer.Change(SERVER_AUTH_CHALLENGE_INTERVAL, Timeout.Infinite);
            }
        }

        private void ResetServerAuthSession()
        {
            mServerAuthTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            mServerAuthSessionKey = string.Empty;
            mServerAuthChallengeKey = string.Empty;
            mLastServerAuthChallenge = string.Empty;
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
                mIncomingDataCallBack ??= OnDataReceived;
                if (mClientSocket == null)
                {
                    return;
                }
                SocketPacket theSockPkt = new SocketPacket
                {
                    ThisSocket = mClientSocket
                };
                mClientSocket.BeginReceive(
                    theSockPkt.DataBuffer,
                    0, theSockPkt.DataBuffer.Length,
                    SocketFlags.None,
                    mIncomingDataCallBack,
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
                    string err = $"BeginReceive failed: ({se.ErrorCode}) {se.Message}";
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

                int bytesReceived = theSockId.ThisSocket.EndReceive(async);
                if (bytesReceived == 0)
                {
                    Disconnect();
                    return;
                }
                char[] chars = new char[bytesReceived + 1];
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
                    string err = $"EndReceive failed: ({se.ErrorCode}) {se.Message}";
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

            data = mPartialPacket + data;
            mPartialPacket = "";

            // Split the data into PDUs.
            string[] packets = data.Split([PDUBase.PACKET_DELIMITER], StringSplitOptions.None);

            // If the last packet has content, it's an incomplete packet.
            int topIndex = packets.Length - 1;
            if (packets[topIndex].Length > 0)
            {
                mPartialPacket = packets[topIndex];
                packets[topIndex] = "";
            }

            // Process each packet.
            foreach (string packet in packets)
            {
                if (packet.Length == 0)
                {
                    continue;
                }

                RaiseRawDataReceived(packet + PDUBase.PACKET_DELIMITER);
                try
                {
                    string[] fields = packet.Split(new[] {PDUBase.DELIMITER}, StringSplitOptions.None);
                    char prefixChar = fields[0][0];
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
                            string pduTypeId = fields[0].Substring(0, 3);
                            fields[0] = fields[0].Substring(3);
                            switch (pduTypeId)
                            {
                                case "$DI":
                                    {
                                        PDUServerIdentification pdu = PDUServerIdentification.Parse(fields);
                                        if (mClientAuth.ClientId != 0)
                                        {
                                            mClientAuthSessionKey = mClientAuth.GenerateAuthResponse(pdu.InitialChallengeKey);
                                            mClientAuthChallengeKey = mClientAuthSessionKey;
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
                                    if (mClientAuth.ClientId != 0)
                                    {
                                        PDUAuthChallenge pdu = PDUAuthChallenge.Parse(fields);
                                        string response = mClientAuth.GenerateAuthResponse(pdu.Challenge, mClientAuthChallengeKey);
                                        mClientAuthChallengeKey = GenerateMd5Digest(mClientAuthSessionKey + response);
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
                                        if (mChallengeServer && mClientAuth.ClientId != 0 && !string.IsNullOrEmpty(mServerAuthChallengeKey) && !string.IsNullOrEmpty(mLastServerAuthChallenge))
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
            string raw = PDUBase.Reassemble(fields);
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

        public static string GenerateMd5Digest(string value)
        {
            byte[] data = Encoding.ASCII.GetBytes(value);
            byte[] result = MD5.HashData(data);
            StringBuilder sb = new StringBuilder();
            foreach (var t in result)
            {
                sb.Append(t.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
