namespace Vatsim.Network;

public class ClientProperties(string name, Version ver)
{
    public string Name { get; } = name;
    public Version Version { get; } = ver;

    public override string ToString()
    {
        return $"{Name} {Version}";
    }
}