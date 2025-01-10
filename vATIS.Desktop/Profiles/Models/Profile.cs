using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.Models;

public class Profile
{
    private const int CurrentVersion = 1;

    [DefaultValue(0)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Version { get; set; } = CurrentVersion;

    public string Name { get; set; } = "";

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? UpdateUrl { get; set; }

    public int? UpdateSerial { get; set; }

    public List<AtisStation>? Stations { get; set; } = [];

    [Obsolete("Use 'Stations' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<AtisStation>? Composites
    {
        get => null;
        set => this.Stations = value;
    }

    public override string ToString()
    {
        return this.Name;
    }
}