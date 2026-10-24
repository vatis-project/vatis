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
    public void Join_EmptyPerItemSeparator_UsesStationSeparator()
    {
        var defs = new[] { Def("A"), Def("B", ""), Def("C") };
        Assert.Equal("A\n\nB\n\nC", StaticDefinitionJoiner.Join(defs, "\\n\\n"));
        Assert.True(StaticDefinitionJoiner.HasCustomSeparators(defs, "\\n\\n"));
        Assert.Equal("A. B. C", StaticDefinitionJoiner.Join(defs, null));
    }

    [Fact]
    public void Join_LastItemSeparator_Ignored()
    {
        var defs = new[] { Def("A"), Def("B", " , ") };
        Assert.Equal("A. B", StaticDefinitionJoiner.Join(defs, null));
        Assert.False(StaticDefinitionJoiner.HasCustomSeparators(defs, null));
    }

    [Theory]
    [InlineData("\\n", "A\nB")]
    [InlineData(".\\n", "A.\nB")]
    [InlineData("\\n\\n", "A\n\nB")]
    public void Join_EscapedLineBreakSeparator_BecomesLineBreak(string separator, string expected)
    {
        Assert.Equal(expected, StaticDefinitionJoiner.Join([Def("A"), Def("B")], separator));
        Assert.Equal(expected, StaticDefinitionJoiner.Join([Def("A", separator), Def("B")], null));
        Assert.True(StaticDefinitionJoiner.HasCustomSeparators([Def("A"), Def("B")], separator));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Finish_TrimsTrailingLineBreaksOnly(bool custom)
    {
        Assert.Equal("A\n\nB.", StaticDefinitionJoiner.Finish("A\n\nB.\r\n\n", custom));
    }

    [Fact]
    public void Join_PeriodAndLineBreakSeparator_DoesNotDoublePeriods()
    {
        var defs = new[] { Def("A."), Def("B."), Def("C.") };
        Assert.Equal("A.\nB.\nC.", StaticDefinitionJoiner.Join(defs, ".\\n", terminate: true));
        Assert.Equal("A.\nB.\nC.", StaticDefinitionJoiner.Join(defs, ".\\n"));
    }

    [Fact]
    public void Join_Terminate_KeepsEverythingButTheTrailingLineBreak()
    {
        var defs = new[] { Def("A"), Def("B") };
        Assert.Equal("A.\nB.", StaticDefinitionJoiner.Join(defs, ".\\n", terminate: true));
        Assert.Equal("A.\nB", StaticDefinitionJoiner.Join(defs, ".\\n"));
        Assert.Equal("A\nB", StaticDefinitionJoiner.Join(defs, "\\n", terminate: true));
        Assert.Equal("A. B", StaticDefinitionJoiner.Join(defs, null, terminate: true));
        Assert.Equal("A,B", StaticDefinitionJoiner.Join(defs, ",", terminate: true));
    }

    [Fact]
    public void Join_BlankLine_AddsEmptyLineBetweenDefinitions()
    {
        var defs = new[] { Def("A"), Def(""), Def("B") };
        Assert.Equal("A.\n\nB", StaticDefinitionJoiner.Join(defs, null));
    }

    [Fact]
    public void Join_BlankLine_KeepsCustomSeparatorPunctuation()
    {
        var defs = new[] { Def("A"), Def(" "), Def("B") };
        Assert.Equal("A,\n\nB", StaticDefinitionJoiner.Join(defs, ", "));
    }

    [Fact]
    public void Join_NewLine_AddsSingleLineBreakBetweenDefinitions()
    {
        var defs = new[] { Def("A"), new StaticDefinition("", 0) { NewLineOnly = true }, Def("B") };
        Assert.Equal("A.\nB", StaticDefinitionJoiner.Join(defs, null));
        Assert.Equal("A,\nB", StaticDefinitionJoiner.Join(defs, ", "));
    }

    [Fact]
    public void Join_NewLineAndBlankLineTogether_AddBothBreaks()
    {
        var newLine = new StaticDefinition("", 0) { NewLineOnly = true };
        Assert.Equal("A.\n\n\nB", StaticDefinitionJoiner.Join([Def("A"), newLine, Def(""), Def("B")], null));
        Assert.Equal("A.\n\nB", StaticDefinitionJoiner.Join([Def("A"), newLine, newLine, Def("B")], null));
        Assert.Equal("A. B", StaticDefinitionJoiner.Join([newLine, Def("A"), Def("B"), newLine], null));
    }

    [Fact]
    public void Join_ConsecutiveBlankLines_AddOneEmptyLineEach()
    {
        var defs = new[] { Def("A"), Def(""), Def(""), Def("B") };
        Assert.Equal("A.\n\n\nB", StaticDefinitionJoiner.Join(defs, null));
    }

    [Fact]
    public void Join_LeadingAndTrailingBlankLines_Ignored()
    {
        var defs = new[] { Def(""), Def("A"), Def("B"), Def("") };
        Assert.Equal("A. B", StaticDefinitionJoiner.Join(defs, null));
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
