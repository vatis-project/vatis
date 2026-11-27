// <copyright file="ReactiveUIInitializer.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Runtime.CompilerServices;
using ReactiveUI.Builder;

namespace Vatsim.Vatis.Tests;

/// <summary>
/// ReactiveUI has to be set up before any view model is created. The app does this at startup.
/// </summary>
internal static class ReactiveUIInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        RxAppBuilder.CreateReactiveUIBuilder()
            .WithCoreServices()
            .BuildApp();
    }
}
