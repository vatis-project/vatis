namespace DevServer.Services;

public interface IMetarRepository
{
    Task<string?> GetVatsimMetar(string id);
}