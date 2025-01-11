// <copyright file="AtisVariable.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Atis;
public class AtisVariable
{
    public string Find { get; set; }
    public string TextReplace { get; set; }
    public string VoiceReplace { get; set; }
    public string[]? Aliases { get; set; }

    public AtisVariable(string find, string textReplace, string voiceReplace, string[]? aliases = null)
    {
        Find = find;
        TextReplace = textReplace;
        VoiceReplace = voiceReplace;
        Aliases = aliases;
    }
}