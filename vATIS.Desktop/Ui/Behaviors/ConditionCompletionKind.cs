// <copyright file="ConditionCompletionKind.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// The kind of condition a <see cref="TextEditorConditionCompletionBehavior"/> suggests keywords for.
/// </summary>
public enum ConditionCompletionKind
{
    /// <summary>
    /// A weather condition.
    /// </summary>
    Weather,

    /// <summary>
    /// A condition on which controllers are online.
    /// </summary>
    Station,
}
