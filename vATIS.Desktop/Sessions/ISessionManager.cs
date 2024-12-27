using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Sessions;

public interface ISessionManager
{
    Profile? CurrentProfile { get; }
    void Run();
    Task StartSession(string profileId);
    void EndSession();
    int MaxConnectionCount { get; }
    int CurrentConnectionCount { get; set; }
}