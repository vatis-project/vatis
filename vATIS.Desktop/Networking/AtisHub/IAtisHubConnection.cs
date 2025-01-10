using System.Threading.Tasks;
using Vatsim.Vatis.Networking.AtisHub.Dto;

namespace Vatsim.Vatis.Networking.AtisHub;

public interface IAtisHubConnection
{
    /// <summary>
    /// Establishes a connection to the ATIS hub.
    /// </summary>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    Task Connect();

    /// <summary>
    /// Disconnects from the ATIS hub.
    /// </summary>
    /// <returns>A task representing the asynchronous disconnection operation.</returns>
    Task Disconnect();

    /// <summary>
    /// Publishes an ATIS to the hub.
    /// </summary>
    /// <param name="dto">The data object containing the ATIS information to be published.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    Task PublishAtis(AtisHubDto dto);

    /// <summary>
    /// Subscribes to a specific ATIS station from the hub.
    /// </summary>
    /// <param name="dto">The request parameters, specifying the ATIS to subscribe to.</param>
    /// <returns>A task representing the asynchronous subscription operation.</returns>
    Task SubscribeToAtis(SubscribeDto dto);

    /// <summary>
    /// Retrieves the current real-world digital ATIS letter.
    /// </summary>
    /// <param name="dto">The request parameters to fetch the digital ATIS letter.</param>
    /// <returns>The ATIS letter if available; otherwise, null.</returns>
    Task<char?> GetDigitalAtisLetter(DigitalAtisRequestDto dto);
}
