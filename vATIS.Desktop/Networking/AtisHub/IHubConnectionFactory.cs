namespace Vatsim.Vatis.Networking.AtisHub;

public interface IHubConnectionFactory
{
    IAtisHubConnection CreateHubConnection();
}