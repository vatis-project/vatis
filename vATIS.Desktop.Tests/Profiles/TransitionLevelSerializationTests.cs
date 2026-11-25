// <copyright file="TransitionLevelSerializationTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Profiles;

public class TransitionLevelSerializationTests
{
    [Fact]
    public void AirportIcaos_RoundTripThroughSourceGeneratedContext()
    {
        var profile = new Profile { Name = "Test", Stations = [new AtisStation { Identifier = "EPWA", Name = "EPWA" }] };
        profile.Stations[0].AtisFormat.TransitionLevel.AirportIcaos = ["EPKK", "EPGD"];

        var json = JsonSerializer.Serialize(profile, SourceGenerationContext.NewDefault.Profile);
        var loaded = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.Profile)!;

        Assert.Equal(["EPKK", "EPGD"], loaded.Stations![0].AtisFormat.TransitionLevel.AirportIcaos);
    }

    [Fact]
    public void LegacyProfile_WithoutAirportIcaos_DefaultsToEmpty()
    {
        var transitionLevel = JsonSerializer.Deserialize<TransitionLevel>("""{"Values":[]}""");

        Assert.NotNull(transitionLevel);
        Assert.Empty(transitionLevel!.AirportIcaos);
    }

    [Fact]
    public void Clone_CopiesAirportIcaos()
    {
        var original = new TransitionLevel { AirportIcaos = ["EPKK"] };

        var clone = original.Clone();
        clone.AirportIcaos.Add("EPGD");

        Assert.Equal(["EPKK"], original.AirportIcaos);
        Assert.Equal(["EPKK", "EPGD"], clone.AirportIcaos);
    }
}
