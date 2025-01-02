namespace Vatsim.Vatis.Voice.Network;

public interface IVoiceServerConnectionFactory
{
    IVoiceServerConnection CreateVoiceServerConnection();
}