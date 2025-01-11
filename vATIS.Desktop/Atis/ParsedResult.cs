namespace Vatsim.Vatis.Atis;

/// <summary>
/// Represents a parsed ATIS result.
/// </summary>
public class ParsedResult
{
    /// <summary>
    /// Gets the text ATIS text response.
    /// </summary>
    public required string TextAtis { get; init; }

    /// <summary>
    /// Gets the voice ATIS text response.
    /// </summary>
    public required string VoiceAtis { get; init; }
}
