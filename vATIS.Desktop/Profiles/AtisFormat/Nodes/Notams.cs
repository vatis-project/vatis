// <copyright file="Notams.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the NOTAMs component of the ATIS format.
/// </summary>
public class Notams : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Notams"/> class.
    /// </summary>
    public Notams()
    {
        Template = new Template
        {
            Text = "NOTAMS... {notams}",
            Voice = "NOTICES TO AIR MISSIONS: {notams}",
        };
    }

    /// <summary>
    /// Creates a new instance of <see cref="Notams"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="Notams"/> instance that is a copy of this instance.</returns>
    public Notams Clone()
    {
        return (Notams)MemberwiseClone();
    }
}
