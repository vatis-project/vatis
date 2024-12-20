using System;
using System.IO;

namespace Vatsim.Vatis.Events;

public class RecordedAtisChanged : EventArgs
{
    public MemoryStream AtisMemoryStream { get; set; }
    public RecordedAtisChanged(MemoryStream stream)
    {
        AtisMemoryStream = stream;
    }
}