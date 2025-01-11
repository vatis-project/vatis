// <copyright file="CompactViewItemViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.ViewModels;

public class CompactViewItemViewModel
{
    public string? Identifier { get; set; }
    public string? AtisLetter { get; set; }
    public string? Wind { get; set; }
    public string? Altimeter { get; set; }
}