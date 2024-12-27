using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Serilog;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Profiles;

public partial class ProfileListDialog : ReactiveWindow<ProfileListViewModel>, IDialogOwner
{
    public ProfileListDialog(ProfileListViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Loaded += ProfileListDialog_Loaded;
    }

    private async void ProfileListDialog_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel?.InitializeCommand.Execute()!;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load profile list");
        }
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    public ProfileListDialog()
    {
        InitializeComponent();
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is ProfileListViewModel model)
        {
            model.SetDialogOwner(this);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        PositionChanged += OnPositionChanged;
        if (DataContext is ProfileListViewModel model)
        {
            model.RestorePosition(this);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (DataContext is ProfileListViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}