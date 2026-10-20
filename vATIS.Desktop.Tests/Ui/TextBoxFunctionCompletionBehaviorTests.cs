using System.Linq;
using Vatsim.Vatis.Ui.Behaviors;
using Xunit;

namespace Vatsim.Vatis.Tests.Ui;

public class TextBoxFunctionCompletionBehaviorTests
{
    [Theory]
    [InlineData("RSC $", 5, 4, "")]
    [InlineData("C Y R $sp", 9, 6, "sp")]
    [InlineData("$phonetic", 9, 0, "phonetic")]
    public void TryGetTrigger_FindsFunctionBeingTyped(string text, int caret, int start, string prefix)
    {
        Assert.True(TextBoxFunctionCompletionBehavior.TryGetTrigger(text, caret, out var s, out var p));
        Assert.Equal((start, prefix), (s, p));
    }

    [Theory]
    [InlineData("$spell($", 8)]
    [InlineData("$1", 2)]
    [InlineData("ABC", 3)]
    [InlineData("$$", 2)]
    [InlineData("", 0)]
    public void TryGetTrigger_IgnoresOtherText(string text, int caret)
    {
        Assert.False(TextBoxFunctionCompletionBehavior.TryGetTrigger(text, caret, out _, out _));
    }

    [Fact]
    public void Filter_MatchesPrefixCaseInsensitively()
    {
        Assert.Equal(["phonetic"], TextBoxFunctionCompletionBehavior.Filter("PH").Select(f => f.Name));
        Assert.Equal(4, TextBoxFunctionCompletionBehavior.Filter(string.Empty).Count);
    }
}
