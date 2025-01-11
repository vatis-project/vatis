// <copyright file="CustomDataGrid.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia;

namespace Vatsim.Vatis.Ui.Controls.DataGrid;

public class CustomDataGrid : Avalonia.Controls.DataGrid
{
    protected override Type StyleKeyOverride => typeof(Avalonia.Controls.DataGrid);

    public static readonly StyledProperty<bool> CanUserAddRowsProperty =
        AvaloniaProperty.Register<Avalonia.Controls.DataGrid, bool>(nameof(CanUserAddRows));

    public bool CanUserAddRows
    {
        get { return GetValue(CanUserAddRowsProperty); }
        set { SetValue(CanUserAddRowsProperty, value); }
    }

    public static readonly StyledProperty<bool> CanUserDeleteRowsProperty =
    AvaloniaProperty.Register<Avalonia.Controls.DataGrid, bool>(nameof(CanUserDeleteRows));

    public bool CanUserDeleteRows
    {
        get { return GetValue(CanUserDeleteRowsProperty); }
        set { SetValue(CanUserDeleteRowsProperty, value); }
    }
}
