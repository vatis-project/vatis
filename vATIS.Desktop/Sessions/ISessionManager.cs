using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Sessions;

public interface ISessionManager
{
    Profile? CurrentProfile { get; }

    int MaxConnectionCount { get; }

    int CurrentConnectionCount { get; set; }

    void Run();

    Task StartSession(string profileId);

    void EndSession();
}