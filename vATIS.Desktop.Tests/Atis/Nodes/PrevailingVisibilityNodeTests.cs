using System.Linq;
using System.Text.Json;
using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class PrevailingVisibilityNodeTests
{
    private const string CavokMetar = "EDDG 011650Z 25009KT CAVOK 15/10 Q1015";

    private static PrevailingVisibilityNode CreateNode(string? cavokText = null, string? cavokVoice = null)
    {
        var station = new AtisStation { Identifier = "EDDG", Name = "EDDG" };
        if (cavokText != null)
            station.AtisFormat.Visibility.CavokText = cavokText;
        if (cavokVoice != null)
            station.AtisFormat.Visibility.CavokVoice = cavokVoice;
        return new PrevailingVisibilityNode { Station = station };
    }

    [Fact]
    public void Cavok_DefaultsToCavok()
    {
        var node = CreateNode();
        var visibility = new MetarDecoder().ParseNotStrict(CavokMetar).Visibility!;

        Assert.Equal("CAVOK", node.ParseTextVariables(visibility, "{visibility}"));
        Assert.Equal("CAVOK", node.ParseVoiceVariables(visibility, "{visibility}"));
    }

    [Fact]
    public void Cavok_UsesCustomText()
    {
        var node = CreateNode("CAVOK.", "clouds and visibility OK");
        var visibility = new MetarDecoder().ParseNotStrict(CavokMetar).Visibility!;

        Assert.Equal("CAVOK.", node.ParseTextVariables(visibility, "{visibility}"));
        Assert.Equal("clouds and visibility OK", node.ParseVoiceVariables(visibility, "{visibility}"));
    }

    private const string DirectionalMetar = "EDDG 011650Z 25009KT 7000 4000E 15/10 Q1015";

    [Fact]
    public void DirectionText_DefaultsToRawValue()
    {
        var node = CreateNode();
        var visibility = new MetarDecoder().ParseNotStrict(DirectionalMetar).Visibility!;

        Assert.Equal("7000 4000E", node.ParseTextVariables(visibility, "{visibility}"));
    }

    [Fact]
    public void DirectionText_UsesCustomText()
    {
        var node = CreateNode();
        node.Station!.AtisFormat.Visibility.DirectionText["E"] = "EAST";
        var visibility = new MetarDecoder().ParseNotStrict(DirectionalMetar).Visibility!;

        Assert.Equal("7000 4000EAST", node.ParseTextVariables(visibility, "{visibility}"));
        Assert.DoesNotContain("EAST", node.ParseVoiceVariables(visibility, "{visibility}"));
    }

    [Fact]
    public void VoiceRules_DefaultsSpeakMetersThenKilometers()
    {
        var node = CreateNode();

        Assert.Equal("eight hundred meters", VoiceFor(node, "0800"));
        Assert.Equal("four thousand niner hundred meters", VoiceFor(node, "4900"));
        Assert.Equal("5 kilometers", VoiceFor(node, "5000"));
        Assert.Equal("6.5 kilometers", VoiceFor(node, "6500"));
        Assert.Equal("7 kilometers", VoiceFor(node, "7000"));
    }

    [Fact]
    public void VoiceRules_AreCustomizable()
    {
        var node = CreateRuleNode((0, "{raw}", "{m} metres"), (1000, "{raw}", "{km} kay em"));

        Assert.Equal("eight hundred metres", VoiceFor(node, "0800"));
        Assert.Equal("7 kay em", VoiceFor(node, "7000"));
    }

    [Fact]
    public void VoiceRules_DirectionalMinimumIsSpokenInGroupForm()
    {
        var node = CreateNode();

        Assert.Equal(
            "7 kilometers and to the east four thousand meters",
            VoiceFor(node, "7000 4000E"));
    }

    [Fact]
    public void VoiceRules_UnlimitedVisibilityUsesOverride()
    {
        var node = CreateNode();

        Assert.Equal("10 kilometers or more", VoiceFor(node, "9999"));
    }

    [Fact]
    public void LegacyVoiceSettings_AreMigratedToRules()
    {
        var json = "{\"MetersCutoff\":3000,\"IncludeVisibilitySuffix\":false,\"MetersUnitVoice\":\"metres\"," +
                   "\"KilometersUnitVoice\":\"kay em\"}";
        var visibility = JsonSerializer.Deserialize<Vatsim.Vatis.Profiles.AtisFormat.Nodes.Visibility>(json, SourceGenerationContext.NewDefault.Options)!;

        Assert.Equal(
            [
                new VisibilityFormatRule(0, "{raw}", "{m}"),
                new VisibilityFormatRule(3001, "{raw}", "{km}"),
                new VisibilityFormatRule(9999, "VIS 10KM", "10 kilometers or more"),
            ],
            visibility.FormatRules);
        Assert.Null(visibility.MetersCutoff);
        Assert.DoesNotContain("MetersCutoff", JsonSerializer.Serialize(visibility, SourceGenerationContext.NewDefault.Options));
    }

    [Fact]
    public void LegacyVoiceSettings_KeepUnitWords()
    {
        var json = "{\"MetersUnitVoice\":\"metres\",\"KilometersUnitVoice\":\"kay em\"}";
        var visibility = JsonSerializer.Deserialize<Vatsim.Vatis.Profiles.AtisFormat.Nodes.Visibility>(json, SourceGenerationContext.NewDefault.Options)!;

        Assert.Equal(
            [
                new VisibilityFormatRule(0, "{raw}", "{m} metres"),
                new VisibilityFormatRule(5001, "{raw}", "{km} kay em"),
                new VisibilityFormatRule(9999, "VIS 10KM", "10 kilometers or more"),
            ],
            visibility.FormatRules);
    }

    [Fact]
    public void LegacyUnlimitedVisibility_DropsTemplateTextAroundTheValue()
    {
        var json = "{\"Template\":{\"Text\":\"VIS {visibility}\",\"Voice\":\"VISIBILITY {visibility}\"}," +
                   "\"UnlimitedVisibilityText\":\"VIS 10KM\",\"UnlimitedVisibilityVoice\":\"visibility 10 kilometers or more\"}";
        var visibility = JsonSerializer.Deserialize<Vatsim.Vatis.Profiles.AtisFormat.Nodes.Visibility>(json, SourceGenerationContext.NewDefault.Options)!;

        Assert.Equal(new VisibilityFormatRule(9999, "10KM", "10 kilometers or more"), visibility.FormatRules[^1]);
        Assert.Null(visibility.UnlimitedVisibilityText);
    }

    [Fact]
    public void FormatRules_RoundTripWithoutMigration()
    {
        var json = "{\"FormatRules\":[{\"FromMeters\":0,\"Text\":\"{m} M\",\"Voice\":\"{m}\"}]}";
        var visibility = JsonSerializer.Deserialize<Vatsim.Vatis.Profiles.AtisFormat.Nodes.Visibility>(json, SourceGenerationContext.NewDefault.Options)!;

        Assert.Equal([new VisibilityFormatRule(0, "{m} M", "{m}")], visibility.FormatRules);
    }

    [Fact]
    public void FractionVoice_DefaultAndCustom()
    {
        var node = CreateNode();
        var visibility = new MetarDecoder().ParseNotStrict("KJFK 011651Z 25009KT 1/2SM CLR 20/10 A3000").Visibility!;
        Assert.Equal("one half.", node.ParseVoiceVariables(visibility, "{visibility}"));

        node.Station!.AtisFormat.Visibility.FractionVoice["1/2SM"] = "half a mile";
        Assert.Equal("half a mile", node.ParseVoiceVariables(visibility, "{visibility}"));
    }

    private static PrevailingVisibilityNode CreateRuleNode(params (int From, string Text, string Voice)[] rules)
    {
        var node = CreateNode();
        node.Station!.AtisFormat.Visibility.FormatRules =
            rules.Select(r => new VisibilityFormatRule(r.From, r.Text, r.Voice)).ToList();
        return node;
    }

    private static PrevailingVisibilityNode CreateTextRuleNode(params (int From, string Text)[] rules)
    {
        return CreateRuleNode(rules.Select(r => (r.From, r.Text, "{m}")).ToArray());
    }

    private static string VoiceFor(PrevailingVisibilityNode node, string vis)
    {
        var visibility = new MetarDecoder().ParseNotStrict($"EDDG 011650Z 25009KT {vis} 15/10 Q1015").Visibility!;
        return node.ParseVoiceVariables(visibility, "{visibility}");
    }

    private static string TextFor(PrevailingVisibilityNode node, string vis)
    {
        var visibility = new MetarDecoder().ParseNotStrict($"EDDG 011650Z 25009KT {vis} 15/10 Q1015").Visibility!;
        return node.ParseTextVariables(visibility, "VIS {visibility}");
    }

    [Theory]
    [InlineData("0800", "VIS 800 M")]
    [InlineData("3000", "VIS 3000 M")]
    [InlineData("5000", "VIS 5 KM")]
    [InlineData("6500", "VIS 6.5 KM")]
    [InlineData("7000", "VIS 7 KM")]
    public void TextRules_SwitchBetweenMetersAndKilometers(string vis, string expected)
    {
        var node = CreateTextRuleNode((0, "{m} M"), (5000, "{km} KM"));

        Assert.Equal(expected, TextFor(node, vis));
    }

    [Fact]
    public void TextRules_DefaultKeepsMetarValue()
    {
        var node = CreateNode();

        Assert.Equal("VIS 0800", TextFor(node, "0800"));
        Assert.Equal("VIS 7000", TextFor(node, "7000"));
    }

    [Fact]
    public void TextRules_BelowLowestRuleFallsBackToMetarValue()
    {
        var node = CreateTextRuleNode((1000, "{km} KM"));

        Assert.Equal("VIS 0800", TextFor(node, "0800"));
        Assert.Equal("VIS 3 KM", TextFor(node, "3000"));
    }

    [Fact]
    public void TextRules_UnlimitedVisibilityIsARow()
    {
        var node = CreateTextRuleNode((0, "{m} M"), (5000, "{km} KM"), (9999, "10KM+"));

        Assert.Equal("VIS 10KM+", TextFor(node, "9999"));
        Assert.Equal("VIS 7 KM", TextFor(node, "7000"));
    }

    [Fact]
    public void TextRules_AppliedToEachValueOfDirectionalMinimum()
    {
        var node = CreateTextRuleNode((0, "{m} M"), (5000, "{km} KM"));

        Assert.Equal("VIS 7 KM 4000 M E", TextFor(node, "7000 4000E"));
    }

    [Fact]
    public void TextRules_DirectionalMinimumKeepsMetarStyleWhenValueEndsInDigit()
    {
        var node = CreateTextRuleNode((0, "{raw}"), (5000, "{km}KM"));

        Assert.Equal("VIS 7KM 4000E", TextFor(node, "7000 4000E"));
    }
}
