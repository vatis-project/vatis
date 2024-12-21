namespace Vatsim.Vatis.Atis;

public record AtisBuilderResponse(string? TextAtis, string? SpokenText, byte[]? AudioBytes);