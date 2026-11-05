// <copyright file="FormattingPage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// A page (tab) of a formatting component.
/// </summary>
/// <param name="Title">The text shown for the page.</param>
/// <param name="Index">The index of the page's tab within the component in the view.</param>
public record FormattingPage(string Title, int Index);
