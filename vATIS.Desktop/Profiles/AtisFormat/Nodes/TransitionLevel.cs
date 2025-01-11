// <copyright file="TransitionLevel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the transition level component of the ATIS format.
/// </summary>
public class TransitionLevel : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionLevel"/> class.
    /// </summary>
    public TransitionLevel()
    {
        Template = new Template { Text = "TRANSITION LEVEL {trl}", Voice = "TRANSITION LEVEL {trl}", };
    }

    /// <summary>
    /// Gets or sets the list of transition level metadata values.
    /// </summary>
    public List<TransitionLevelMeta> Values { get; set; } = new();

    /// <summary>
    /// Creates a new instance of <see cref="TransitionLevel"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="TransitionLevel"/> instance that is a copy of this instance.</returns>
    public TransitionLevel Clone()
    {
        return (TransitionLevel)MemberwiseClone();
    }
}
