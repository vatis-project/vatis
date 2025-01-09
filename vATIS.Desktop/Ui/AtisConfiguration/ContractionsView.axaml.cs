using System;
using System.Linq;
using Avalonia.Controls;
using Slugify;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

public partial class ContractionsView : UserControl
{
    private static readonly SlugHelper s_slug = new();

    public ContractionsView()
    {
        InitializeComponent();
    }

    private void Contractions_OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (DataContext is ContractionsViewModel vm)
        {
            if (e.Row.DataContext is ContractionMeta row)
            {
                vm.CurrentContractions.Add(new Tuple<int, ContractionMeta>(e.Row.Index, row));
            }
        }
    }

    private void Contractions_Validate(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (sender is not DataGrid dataGrid || e.EditAction != DataGridEditAction.Commit)
            return;

        if (DataContext is ContractionsViewModel vm)
        {
            if (e.EditingElement is TextBox textBox)
            {
                if (e.Column.Header.ToString() == "Variable")
                {
                    var slug = s_slug.GenerateSlug(textBox.Text).Replace("-", "_").ToUpperInvariant();

                    if (vm.CurrentContractions.Any(x => x.Item1 != e.Row.Index && string.Equals(x.Item2.VariableName,
                            slug, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        e.Cancel = true;
                        dataGrid.CancelEdit();
                    }

                    textBox.Text = slug;
                }
            }
        }
    }
}
