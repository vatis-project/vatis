using Avalonia.Input;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents a message sent over the websocket with ATIS information.
/// </summary>
public class AtisMessage()
{
  /// <summary>
  /// Represents the value of an ATIS message.
  /// </summary>
  public class AtisMessageValue(string stationId, AtisType atisType, char atisLetter, string? metar, string? wind, string? altimeter, bool isPublished) : AtisHubDto(stationId, atisType, atisLetter, metar, wind, altimeter)
  {
    /// <summary>
    /// Gets a value indicating whether the ATIS message is published.
    /// </summary>
    public bool IsPublished { get; } = isPublished;
  }

  /// <summary>
  /// Gets the key identifying the message as an ATIS message.
  /// </summary>
  public string Key { get; } = "Atis";

  /// <summary>
  /// Gets or sets the ATIS information.
  /// </summary>
  public AtisMessageValue? Value { get; set; }
}