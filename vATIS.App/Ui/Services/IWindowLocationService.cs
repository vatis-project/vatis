using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Services;

public interface IWindowLocationService
{
    void Restore(Window? window);
    void Update(Window? window);
}