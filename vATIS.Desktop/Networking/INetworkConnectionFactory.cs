using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking;
public interface INetworkConnectionFactory
{
    NetworkConnection CreateConnection(AtisStation station);
}
