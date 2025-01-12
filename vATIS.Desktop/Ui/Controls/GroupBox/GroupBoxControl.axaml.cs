// <copyright file="GroupBoxControl.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls.Primitives;

namespace Vatsim.Vatis.Ui.Controls.GroupBox;

/// <summary>
/// Represents a customizable group box control that inherits from <see cref="HeaderedContentControl"/>
/// and provides functionality for grouping related UI elements with an optional header.
/// </summary>
public partial class GroupBoxControl : HeaderedContentControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupBoxControl"/> class.
    /// </summary>
    public GroupBoxControl()
    {
        InitializeComponent();
    }
}
