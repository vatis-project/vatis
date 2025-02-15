using ReactiveUI;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the <see cref="ReleaseNotesDialog"/>.
/// </summary>
public class ReleaseNotesDialogViewModel : ReactiveViewModelBase
{
    private string? _releaseNotes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseNotesDialogViewModel"/> class.
    /// </summary>
    public ReleaseNotesDialogViewModel()
    {
    }

    /// <summary>
    /// Gets or sets the release notes markdown text.
    /// </summary>
    public string? ReleaseNotes
    {
        get => _releaseNotes;
        set => this.RaiseAndSetIfChanged(ref _releaseNotes, value);
    }
}
