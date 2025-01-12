// <copyright file="CompactViewItemViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for a compact view item, containing properties to display specific ATIS-related information.
/// </summary>
public class CompactViewItemViewModel
{
    /// <summary>
    /// Gets or sets the station identifier associated with the compact view item.
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Gets or sets the ATIS letter associated with the compact view item.
    /// </summary>
    public string? AtisLetter { get; set; }

    /// <summary>
    /// Gets or sets the wind information associated with the compact view item.
    /// </summary>
    public string? Wind { get; set; }

    /// <summary>
    /// Gets or sets the altimeter information associated with the compact view item.
    /// </summary>
    public string? Altimeter { get; set; }
}
