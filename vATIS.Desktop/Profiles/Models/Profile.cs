using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.Models;

public class Profile
{
    private const int CURRENT_VERSION = 1;

    [DefaultValue(0)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Version { get; set; } = CURRENT_VERSION;

    public string Name { get; set; } = "";
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? UpdateUrl { get; set; }
    public int? UpdateSerial { get; set; }
    public List<AtisStation>? Stations { get; set; } = [];

    [Obsolete("Use 'Stations' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<AtisStation>? Composites
    {
        get => default;
        set => Stations = value;
    }

    public override string? ToString() => Name;
}