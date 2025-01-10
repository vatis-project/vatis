using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class PointerPressedEventTrigger : Trigger<Interactive>
{
    public static readonly StyledProperty<ICommand> LeftClickCommandProperty =
        AvaloniaProperty.Register<PointerPressedEventTrigger, ICommand>(nameof(LeftClickCommand));

    public static readonly StyledProperty<ICommand> RightClickCommandProperty =
        AvaloniaProperty.Register<PointerPressedEventTrigger, ICommand>(nameof(RightClickCommand));

    public ICommand LeftClickCommand
    {
        get => this.GetValue(LeftClickCommandProperty);
        set => this.SetValue(LeftClickCommandProperty, value);
    }

    public ICommand RightClickCommand
    {
        get => this.GetValue(RightClickCommandProperty);
        set => this.SetValue(RightClickCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject?.AddHandler(InputElement.PointerPressedEvent, this.Handler, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.AssociatedObject?.RemoveHandler(InputElement.PointerPressedEvent, this.Handler);
    }

    private void Handler(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(null);
        if (point.Properties.IsLeftButtonPressed)
        {
            this.LeftClickCommand.Execute(null);
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            this.RightClickCommand.Execute(null);
        }
    }
}