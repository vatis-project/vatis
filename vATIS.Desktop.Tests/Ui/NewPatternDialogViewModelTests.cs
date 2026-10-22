using System;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Ui.ViewModels;
using Xunit;

namespace Vatsim.Vatis.Tests.Ui;

public class NewPatternDialogViewModelTests
{
    private const string Pattern = @"RSC (\d+) (\d)/(\d)/(\d)";

    [Fact]
    public void SampleText_IsGeneratedFromThePattern()
    {
        using var vm = new NewPatternDialogViewModel { Pattern = Pattern, Text = "$0", Spoken = "$0" };

        Assert.False(string.IsNullOrEmpty(vm.SampleText));
        Assert.Matches(new Regex(Pattern), vm.SampleText!);
        Assert.True(vm.HasPreview);
    }

    [Fact]
    public void SampleText_TypedByTheUserIsKeptWhenThePatternChanges()
    {
        using var vm = new NewPatternDialogViewModel { Pattern = Pattern };

        vm.SampleText = "RSC 23 5/5/5";
        vm.Pattern = @"RSC (\d+)";

        Assert.Equal("RSC 23 5/5/5", vm.SampleText);
    }

    [Fact]
    public void SampleText_ResumesAfterTheUserClearsIt()
    {
        using var vm = new NewPatternDialogViewModel { Pattern = Pattern };
        vm.SampleText = "typed";
        vm.SampleText = string.Empty;

        vm.Pattern = @"PAPI";

        Assert.Equal("PAPI", vm.SampleText);
    }

    [Fact]
    public void SampleText_IsClearedWhenThePatternIsInvalid()
    {
        using var vm = new NewPatternDialogViewModel { Pattern = Pattern };

        vm.Pattern = "(";

        Assert.Empty(vm.SampleText!);
        Assert.False(vm.HasPreview);
        Assert.Equal("Fix the pattern to see a preview.", vm.PreviewStatus);
    }

    [Fact]
    public void NewSampleCommand_ReplacesATypedSampleWithAGeneratedOne()
    {
        using var vm = new NewPatternDialogViewModel { Pattern = Pattern };
        vm.SampleText = "typed";

        vm.NewSampleCommand.Execute().Subscribe();

        Assert.Matches(new Regex(Pattern), vm.SampleText!);
    }
}
