using System.Text.Json.Serialization;
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
  public class AtisMessageValue(string stationId, AtisType atisType, char atisLetter, string? metar, string? wind, string? altimeter, string? textAtis, bool isNewAtis, bool isPublished)
  {
    /// <summary>
    /// Gets a value indicating whether the ATIS message is published.
    /// </summary>
    [JsonPropertyName("isPublished")]
    public bool IsPublished { get; } = isPublished;

    /// <summary>
    /// Gets the ATIS message text.
    /// </summary>
    [JsonPropertyName("textAtis")]
    public string? TextAtis { get; } = textAtis;

    /// <summary>
    /// Gets the station ID of the ATIS message.
    /// </summary>
    [JsonPropertyName("stationId")]
    public string StationId { get; } = stationId;

    /// <summary>
    /// Gets the type of the ATIS message.
    /// </summary>
    [JsonPropertyName("atisType")]
    public AtisType AtisType { get; } = atisType;

    /// <summary>
    /// Gets the ATIS letter.
    /// </summary>
    [JsonPropertyName("atisLetter")]
    public char AtisLetter { get; } = atisLetter;

    /// <summary>
    /// Gets the METAR used to create the ATIS.
    /// </summary>
    [JsonPropertyName("metar")]
    public string? Metar { get; } = metar;

    /// <summary>
    /// Gets the current winds.
    /// </summary>
    [JsonPropertyName("wind")]
    public string? Wind { get; } = wind;

    /// <summary>
    /// Gets the current altimeter.
    /// </summary>
    [JsonPropertyName("altimeter")]
    public string? Altimeter { get; } = altimeter;

    /// <summary>
    /// Gets a value indicating whether the ATIS message is new.
    /// </summary>
    [JsonPropertyName("isNewAtis")]
    public bool IsNewAtis { get; } = isNewAtis;
  }

  /// <summary>
  /// Gets the string identifying the message as an ATIS message.
  /// </summary>
  [JsonPropertyName("type")]
  public string MessageType { get; } = "atis";

  /// <summary>
  /// Gets or sets the ATIS information.
  /// </summary>
  [JsonPropertyName("value")]
  public AtisMessageValue? Value { get; set; }
}