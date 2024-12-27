using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Vatsim.Network;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Updates;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    Converters = [typeof(JsonStringEnumConverter<NetworkRating>)]
)]
[JsonSerializable(typeof(NetworkRating))]
[JsonSerializable(typeof(AppConfiguration))]
[JsonSerializable(typeof(VatsimStatus))]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(AvailableNavData))]
[JsonSerializable(typeof(List<Airport>))]
[JsonSerializable(typeof(List<Navaid>))]
[JsonSerializable(typeof(List<VoiceMetaData>))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(TextToSpeechRequestDto))]
[JsonSerializable(typeof(Clouds))]
[JsonSerializable(typeof(UndeterminedLayer))]
[JsonSerializable(typeof(CloudType))]
[JsonSerializable(typeof(Dictionary<string, CloudType>))]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(ContractionMeta))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(AtisStation))]
[JsonSerializable(typeof(ClientVersionInfo))]
[JsonSerializable(typeof(PostUserRequestDto))]
[JsonSerializable(typeof(PutBotRequestDto))]
[JsonSerializable(typeof(IdsUpdateRequest))]
[JsonSerializable(typeof(SubscribeDto))]
[JsonSerializable(typeof(List<AtisHubDto>))]
[JsonSerializable(typeof(JsonElement))]
public partial class SourceGenerationContext : JsonSerializerContext
{
    public static SourceGenerationContext NewDefault { get; } = new(new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
}