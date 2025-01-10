namespace Vatsim.Vatis.Ui.Common;

public class ComboBoxItemMeta
{
    public ComboBoxItemMeta(string display, string value)
    {
        this.Display = display;
        this.Value = value;
    }

    public string Display { get; set; }

    public string Value { get; set; }
}