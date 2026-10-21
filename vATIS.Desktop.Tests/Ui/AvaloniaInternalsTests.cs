using System.Reflection;
using Avalonia.Controls;
using Xunit;

namespace Vatsim.Vatis.Tests.Ui;

public class AvaloniaInternalsTests
{
    [Fact]
    public void TopLevel_StillHasTheToolTipServiceFieldTheMacOsFixClears()
    {
        // ModalToolTipFallback clears this private field to stop tooltips in macOS dialogs closing early. If an Avalonia
        // upgrade renames or removes it, revisit that workaround.
        var field = typeof(TopLevel).GetField("_tooltipService", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
    }
}
