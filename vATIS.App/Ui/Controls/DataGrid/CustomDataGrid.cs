using System;
using Avalonia;
using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Controls;

public class CustomDataGrid : DataGrid
{
    protected override Type StyleKeyOverride => typeof(DataGrid);

    public static readonly StyledProperty<bool> CanUserAddRowsProperty =
        AvaloniaProperty.Register<DataGrid, bool>(nameof(CanUserAddRows));

    public bool CanUserAddRows
    {
        get { return GetValue(CanUserAddRowsProperty); }
        set { SetValue(CanUserAddRowsProperty, value); }
    }

    public static readonly StyledProperty<bool> CanUserDeleteRowsProperty =
    AvaloniaProperty.Register<DataGrid, bool>(nameof(CanUserDeleteRows));

    public bool CanUserDeleteRows
    {
        get { return GetValue(CanUserDeleteRowsProperty); }
        set { SetValue(CanUserDeleteRowsProperty, value); }
    }

    protected override void OnRowEditEnding(DataGridRowEditEndingEventArgs e)
    {
        base.OnRowEditEnding(e);
    }
}
