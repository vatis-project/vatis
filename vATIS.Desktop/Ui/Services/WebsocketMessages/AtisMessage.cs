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
  public class AtisMessageValue(string stationId, AtisType atisType, char atisLetter, string? metar, string? wind, string? altimeter, bool isNewAtis, bool isPublished)
  {
    [JsonPropertyName("isPublished")]
    /// <summary>
    /// Gets a value indicating whether the ATIS message is published.
    /// </summary>
    public bool IsPublished { get; } = isPublished;

    [JsonPropertyName("stationId")]
    /// <summary>
    /// Gets the station ID of the ATIS message.
    /// </summary>
    public string StationId { get; } = stationId;

    [JsonPropertyName("atisType")]
    /// <summary>
    /// Gets the type of the ATIS message.
    /// </summary>
    public AtisType AtisType { get; } = atisType;

    [JsonPropertyName("atisLetter")]
    /// <summary>
    /// Gets the ATIS letter.
    /// </summary>
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

    [JsonPropertyName("isNewAtis")]
    /// <summary>
    /// Gets a value indicating whether the ATIS message is new.
    /// </summary>
    public bool IsNewAtis { get; } = isNewAtis;
  }

  [JsonPropertyName("type")]
  /// <summary>
  /// Gets the string identifying the message as an ATIS message.
  /// </summary>
  public string MessageType { get; } = "atis";

  [JsonPropertyName("value")]
  /// <summary>
  /// Gets or sets the ATIS information.
  /// </summary>
  public AtisMessageValue? Value { get; set; }
}