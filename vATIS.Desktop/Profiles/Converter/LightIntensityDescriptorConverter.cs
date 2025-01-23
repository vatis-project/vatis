// <copyright file="LightIntensityDescriptorConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Converter;

/// <summary>
/// Converts legacy "light intensity" weather descriptor.
/// </summary>
public class LightIntensityDescriptorConverter : TemplateConverterBase
{
    /// <inheritdoc />
    protected override string DefaultText => "-";

    /// <inheritdoc />
    protected override string DefaultVoice => "light";
}
