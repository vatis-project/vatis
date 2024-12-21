namespace Vatsim.Vatis.Ui.Common;
public class ComboBoxItemMeta
{
    public string Display { get; set; }
    public string Value { get; set; }

    public ComboBoxItemMeta(string display, string value)
    {
        Display = display;
        Value = value;
    }
}