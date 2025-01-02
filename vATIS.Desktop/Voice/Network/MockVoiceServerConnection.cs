using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis.Voice.Network;

public class MockVoiceServerConnection : IVoiceServerConnection
{
    public Task Connect(string username, string password)
    {
        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        
    }

    public Task AddOrUpdateBot(string callsign, PutBotRequestDto dto, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task RemoveBot(string callsign)
    {
        return Task.CompletedTask;
    }
}