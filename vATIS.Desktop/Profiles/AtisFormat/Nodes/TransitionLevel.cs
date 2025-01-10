using System.Collections.Generic;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

public class TransitionLevel : BaseFormat
{
    public TransitionLevel()
    {
        this.Template = new Template
        {
            Text = "TRANSITION LEVEL {trl}",
            Voice = "TRANSITION LEVEL {trl}"
        };
    }

    public List<TransitionLevelMeta> Values { get; set; } = [];

    public TransitionLevel Clone()
    {
        return (TransitionLevel)this.MemberwiseClone();
    }
}