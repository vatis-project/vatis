// <copyright file="VicinityDescriptorConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Converter;

/// <summary>
/// Converts legacy "vicinity" weather proximity descriptor.
/// </summary>
public class VicinityDescriptorConverter : TemplateConverterBase
{
    /// <inheritdoc />
    protected override string DefaultText => "VC";

    /// <inheritdoc />
    protected override string DefaultVoice => "in vicinity";
}
