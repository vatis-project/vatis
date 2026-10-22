using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Profiles;

public class ContractionMetaTests
{
    [Fact]
    public void Clone_CopiesVariableName()
    {
        var clone = new ContractionMeta { VariableName = "V", Text = "t", Voice = "v" }.Clone();

        Assert.Equal(("V", "t", "v"), (clone.VariableName, clone.Text, clone.Voice));
    }
}
