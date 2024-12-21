using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Slugify;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class NewContractionDialog : ReactiveWindow<NewContractionDialogViewModel>, ICloseable
{
    private static readonly SlugHelper Slug = new();
    
    public NewContractionDialog(NewContractionDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public NewContractionDialog()
    {
        InitializeComponent();
    }
    
    private void Variable_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox textBox)
        {
            textBox.Text = Slug.GenerateSlug(textBox.Text).ToUpperInvariant().Replace("-", "_");
        }
    }
}