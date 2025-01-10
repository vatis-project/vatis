using System;
using Avalonia;

namespace Vatsim.Vatis.Ui.Controls.DataGrid;

public class CustomDataGrid : Avalonia.Controls.DataGrid
{
    public static readonly StyledProperty<bool> CanUserAddRowsProperty =
        AvaloniaProperty.Register<Avalonia.Controls.DataGrid, bool>(nameof(CanUserAddRows));

    public static readonly StyledProperty<bool> CanUserDeleteRowsProperty =
        AvaloniaProperty.Register<Avalonia.Controls.DataGrid, bool>(nameof(CanUserDeleteRows));

    protected override Type StyleKeyOverride => typeof(Avalonia.Controls.DataGrid);

    public bool CanUserAddRows
    {
        get => this.GetValue(CanUserAddRowsProperty);
        set => this.SetValue(CanUserAddRowsProperty, value);
    }

    public bool CanUserDeleteRows
    {
        get => this.GetValue(CanUserDeleteRowsProperty);
        set => this.SetValue(CanUserDeleteRowsProperty, value);
    }
}