// <copyright file="IWindowLocationService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Services;

public interface IWindowLocationService
{
    void Restore(Window? window);
    void Update(Window? window);
}