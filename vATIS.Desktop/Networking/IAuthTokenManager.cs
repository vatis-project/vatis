using System.Threading.Tasks;

namespace Vatsim.Vatis.Networking;
public interface IAuthTokenManager
{
    Task<string?> GetAuthToken();
    string? AuthToken { get; }
}
