// <copyright file="StaticDefinitionJoinerTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class StaticDefinitionJoinerTests
{
    private static StaticDefinition Def(string text, string? after = null)
    {
        return new StaticDefinition(text, 0) { SeparatorAfter = after };
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(". ")]
    public void Join_DefaultSeparator(string? separator)
    {
        Assert.Equal("A. B", StaticDefinitionJoiner.Join([Def("A"), Def("B")], separator));
    }

    [Fact]
    public void Join_StationSeparator_PreservedExactly()
    {
        Assert.Equal("A , B", StaticDefinitionJoiner.Join([Def("A"), Def("B")], " , "));
    }

    [Fact]
    public void Join_PerItemSeparator_OverridesStation()
    {
        var defs = new[] { Def("A", " , "), Def("B"), Def("C", " / ") };
        Assert.Equal("A , B. C", StaticDefinitionJoiner.Join(defs, null));
        Assert.True(StaticDefinitionJoiner.HasCustomSeparators(defs, null));
    }

    [Fact]
    public void Join_LastItemSeparator_Ignored()
    {
        var defs = new[] { Def("A"), Def("B", " , ") };
        Assert.Equal("A. B", StaticDefinitionJoiner.Join(defs, null));
        Assert.False(StaticDefinitionJoiner.HasCustomSeparators(defs, null));
    }

    [Fact]
    public void Join_TrimTrailingPeriod()
    {
        Assert.Equal("A. B", StaticDefinitionJoiner.Join([Def("A."), Def("B.")], null, trimTrailingPeriod: true));
    }

    [Fact]
    public void Finish_AndPrepare_RespectCustomFlag()
    {
        Assert.Equal("A , B", StaticDefinitionJoiner.Finish("A , B", true));
        Assert.Equal("A, B.", StaticDefinitionJoiner.Finish("A , B..", false));
        Assert.Equal("X.", StaticDefinitionJoiner.Prepare("X ..", true));
        Assert.Equal("X ..", StaticDefinitionJoiner.Prepare("X ..", false));
    }
}
