using System;
using System.Text.Json;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Services.Websocket.Messages;
using Xunit;

namespace Vatsim.Vatis.Tests.Websocket;

public class SetAtisLetterMessageTests
{
    private static readonly CodeRangeMeta FullRange = new('A', 'Z');

    private static SetAtisLetterMessage.SetAtisLetterMessagePayload Message(string value)
    {
        var json = """{"type":"setAtisLetter","value":""" + value + "}";
        var message = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.SetAtisLetterMessage);
        Assert.NotNull(message?.Payload);
        return message.Payload;
    }

    [Fact]
    public void Deserialize_WithStationAndLetter_DefaultsToCombined()
    {
        var payload = Message("""{"station":"KDEN","letter":"D"}""");

        Assert.Equal("KDEN", payload.Station);
        Assert.Equal(AtisType.Combined, payload.AtisType);
        Assert.Equal("D", payload.Letter);
        Assert.Null(payload.Step);
    }

    [Theory]
    [InlineData("Next", AtisLetterStep.Next)]
    [InlineData("Previous", AtisLetterStep.Previous)]
    public void Deserialize_WithStep_ReadsTheStep(string step, AtisLetterStep expected)
    {
        var payload = Message("""{"station":"KSFO","atisType":"Departure","step":"STEP"}""".Replace("STEP", step));

        Assert.Equal(AtisType.Departure, payload.AtisType);
        Assert.Equal(expected, payload.Step);
        Assert.Null(payload.Letter);
    }

    [Theory]
    [InlineData("""{"letter":"D"}""")]
    [InlineData("""{"id":"abc","station":"KDEN","letter":"D"}""")]
    [InlineData("""{"station":"KDEN"}""")]
    [InlineData("""{"station":"KDEN","letter":"D","step":"Next"}""")]
    [InlineData("""{"station":"KDEN","letter":"DE"}""")]
    [InlineData("""{"station":"KDEN","letter":""}""")]
    [InlineData("""{"station":"KDEN","letter":"1"}""")]
    public void Validate_WithInvalidPayload_Throws(string value)
    {
        var payload = Message(value);

        Assert.Throws<ArgumentException>(payload.Validate);
    }

    [Theory]
    [InlineData("""{"id":"abc","letter":"D"}""")]
    [InlineData("""{"station":"KDEN","step":"Next"}""")]
    public void Validate_WithValidPayload_DoesNotThrow(string value)
    {
        var payload = Message(value);

        payload.Validate();
    }

    [Fact]
    public void Resolve_WithLowercaseLetter_ReturnsUppercase()
    {
        var payload = new SetAtisLetterMessage.SetAtisLetterMessagePayload { Letter = "d" };

        Assert.Equal('D', payload.Resolve('A', FullRange));
    }

    [Fact]
    public void Resolve_WithLetterOutsideCodeRange_Throws()
    {
        var payload = new SetAtisLetterMessage.SetAtisLetterMessagePayload { Letter = "Q" };

        Assert.Throws<ArgumentException>(() => payload.Resolve('A', new CodeRangeMeta('A', 'M')));
    }

    [Theory]
    [InlineData('D', 'E')]
    [InlineData('Z', 'A')]
    public void Resolve_WithNextStep_MovesForwardAndWraps(char current, char expected)
    {
        var payload = new SetAtisLetterMessage.SetAtisLetterMessagePayload { Step = AtisLetterStep.Next };

        Assert.Equal(expected, payload.Resolve(current, FullRange));
    }

    [Theory]
    [InlineData('D', 'C')]
    [InlineData('A', 'Z')]
    public void Resolve_WithPreviousStep_MovesBackAndWraps(char current, char expected)
    {
        var payload = new SetAtisLetterMessage.SetAtisLetterMessagePayload { Step = AtisLetterStep.Previous };

        Assert.Equal(expected, payload.Resolve(current, FullRange));
    }

    [Theory]
    [InlineData(AtisLetterStep.Next, 'Q', 'A')]
    [InlineData(AtisLetterStep.Previous, 'Q', 'M')]
    public void Resolve_WithCurrentLetterOutsideCodeRange_StartsFromTheEdge(AtisLetterStep step, char current,
        char expected)
    {
        var payload = new SetAtisLetterMessage.SetAtisLetterMessagePayload { Step = step };

        Assert.Equal(expected, payload.Resolve(current, new CodeRangeMeta('A', 'M')));
    }
}
