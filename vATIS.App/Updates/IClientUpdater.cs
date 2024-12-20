using System.Threading.Tasks;

namespace Vatsim.Vatis.Updates;

public interface IClientUpdater
{
    Task<bool> Run();
}