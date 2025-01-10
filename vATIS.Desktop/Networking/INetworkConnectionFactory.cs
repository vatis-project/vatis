using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking;

public interface INetworkConnectionFactory
{
    INetworkConnection CreateConnection(AtisStation station);
}