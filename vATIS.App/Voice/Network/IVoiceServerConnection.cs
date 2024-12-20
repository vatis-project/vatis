using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis.Voice.Network;

public interface IVoiceServerConnection
{
    Task Connect(string username, string password);
    void Disconnect();
    Task AddOrUpdateBot(string callsign, PutBotRequestDto dto, CancellationToken cancellationToken);
    Task RemoveBot(string callsign);
}