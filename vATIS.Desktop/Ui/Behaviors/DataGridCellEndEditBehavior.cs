using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class DataGridCellEndEditBehavior : Behavior<DataGrid>
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<DataGridCellEndEditBehavior, ICommand?>(nameof(Command));

    public ICommand? Command
    {
        get => this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (this.AssociatedObject != null)
        {
            this.AssociatedObject.CellEditEnding += this.AssociatedObjectOnCellEditEnding;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (this.AssociatedObject != null)
        {
            this.AssociatedObject.CellEditEnding -= this.AssociatedObjectOnCellEditEnding;
        }
    }

    private void AssociatedObjectOnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        this.Command?.Execute(e);
    }
}