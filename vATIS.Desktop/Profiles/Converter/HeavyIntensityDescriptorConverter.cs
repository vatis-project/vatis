// <copyright file="HeavyIntensityDescriptorConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Converter;

/// <summary>
/// Converts legacy heavy intensity descriptor to the new format.
/// </summary>
public class HeavyIntensityDescriptorConverter : TemplateConverterBase
{
    /// <inheritdoc />
    protected override string DefaultText => "+";

    /// <inheritdoc />
    protected override string DefaultVoice => "heavy";
}
