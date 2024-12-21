using System.Threading.Tasks;

namespace Vatsim.Vatis.NavData;
public interface INavDataRepository
{
    Task Initialize();
    Task CheckForUpdates();
    Airport? GetAirport(string id);
    Navaid? GetNavaid(string id);
}
