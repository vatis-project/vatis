// <copyright file="AtisLetterStep.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// The direction to move an ATIS letter in.
/// </summary>
public enum AtisLetterStep
{
    /// <summary>
    /// The next letter in the code range.
    /// </summary>
    Next,

    /// <summary>
    /// The previous letter in the code range.
    /// </summary>
    Previous,
}
