using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Vatsim.Vatis.Ui.Controls;

public class NonNavigableComboBox : ComboBox
{
    protected override Type StyleKeyOverride => typeof(ComboBox);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down)
        {
            // Disable up/down arrow navigation
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }
}