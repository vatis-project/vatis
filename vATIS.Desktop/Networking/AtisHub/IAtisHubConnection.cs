using System.Threading.Tasks;
using Vatsim.Vatis.Networking.AtisHub.Dto;

namespace Vatsim.Vatis.Networking.AtisHub;

public interface IAtisHubConnection
{
    Task Connect();
    Task Disconnect();
    Task PublishAtis(AtisHubDto dto);
    Task SubscribeToAtis(SubscribeDto dto);
    Task<char> GetDigitalAtisLetter(DigitalAtisRequestDto dto);
}