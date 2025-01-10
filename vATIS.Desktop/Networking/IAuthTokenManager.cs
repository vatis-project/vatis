using System.Threading.Tasks;

namespace Vatsim.Vatis.Networking;

public interface IAuthTokenManager
{
    string? AuthToken { get; }

    Task<string?> GetAuthToken();
}