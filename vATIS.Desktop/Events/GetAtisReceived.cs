using Vatsim.Vatis.Profiles.Models;
using WatsonWebsocket;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Event arguments for the GetAllAtisReceived event.
/// </summary>
/// <param name="Session">The client that requested the ATIS.</param>
/// <param name="Station">The station requested. If null every station was requested.</param>
/// <param name="AtisType">The ATIS type requested.</param>
public record GetAtisReceived(ClientMetadata Session, string? Station, AtisType? AtisType = AtisType.Combined) : IEvent;
