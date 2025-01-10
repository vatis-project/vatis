using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Editing;
using ReactiveUI;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Controls;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Models;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

public class FormattingViewModel : ReactiveViewModelBase
{
    private readonly HashSet<string> _initializedProperties = [];
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;

    public FormattingViewModel(
        IWindowFactory windowFactory,
        IProfileRepository profileRepository,
        ISessionManager sessionManager)
    {
        this._windowFactory = windowFactory;
        this._profileRepository = profileRepository;
        this._sessionManager = sessionManager;

        this.FormattingOptions = [];

        this.AtisStationChanged = ReactiveCommand.Create<AtisStation>(this.HandleAtisStationChanged);
        this.TemplateVariableClicked = ReactiveCommand.Create<string>(this.HandleTemplateVariableClicked);
        this.CellEditEndingCommand = ReactiveCommand.Create<DataGridCellEditEndingEventArgs>(this.HandleCellEditEnding);
        this.AddTransitionLevelCommand = ReactiveCommand.CreateFromTask(this.HandleAddTransitionLevel);
        this.DeleteTransitionLevelCommand =
            ReactiveCommand.CreateFromTask<TransitionLevelMeta>(this.HandleDeleteTransitionLevel);
    }

    public IDialogOwner? DialogOwner { get; set; }

    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    public ReactiveCommand<string, Unit> TemplateVariableClicked { get; }

    public ReactiveCommand<DataGridCellEditEndingEventArgs, Unit> CellEditEndingCommand { get; }

    public ReactiveCommand<Unit, Unit> AddTransitionLevelCommand { get; }

    public ReactiveCommand<TransitionLevelMeta, Unit> DeleteTransitionLevelCommand { get; }

    private void HandleAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        this.SelectedStation = station;

        this.FormattingOptions = [];
        if (station.IsFaaAtis)
        {
            this.FormattingOptions =
            [
                "Observation Time",
                "Wind",
                "Visibility",
                "Weather",
                "Clouds",
                "Temperature",
                "Dewpoint",
                "Altimeter",
                "NOTAMs",
                "Closing Statement"
            ];
        }
        else
        {
            this.FormattingOptions =
            [
                "Observation Time",
                "Wind",
                "Visibility",
                "Weather",
                "Clouds",
                "Temperature",
                "Dewpoint",
                "Altimeter",
                "Transition Level",
                "NOTAMs",
                "Closing Statement"
            ];
        }

        this.SpeakWindSpeedLeadingZero = station.AtisFormat.SurfaceWind.SpeakLeadingZero;
        this.MagneticVariationEnabled = station.AtisFormat.SurfaceWind.MagneticVariation.Enabled;
        this.MagneticVariationValue = station.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees.ToString();
        this.RoutineObservationTime = string.Join(",", station.AtisFormat.ObservationTime.StandardUpdateTime ?? []);
        this.ObservationTimeTextTemplate = station.AtisFormat.ObservationTime.Template.Text;
        this.ObservationTimeVoiceTemplate = station.AtisFormat.ObservationTime.Template.Voice;
        this.StandardWindTextTemplate = station.AtisFormat.SurfaceWind.Standard.Template.Text;
        this.StandardWindVoiceTemplate = station.AtisFormat.SurfaceWind.Standard.Template.Voice;
        this.StandardGustWindTextTemplate = station.AtisFormat.SurfaceWind.StandardGust.Template.Text;
        this.StandardGustWindVoiceTemplate = station.AtisFormat.SurfaceWind.StandardGust.Template.Voice;
        this.VariableWindTextTemplate = station.AtisFormat.SurfaceWind.Variable.Template.Text;
        this.VariableWindVoiceTemplate = station.AtisFormat.SurfaceWind.Variable.Template.Voice;
        this.VariableGustWindTextTemplate = station.AtisFormat.SurfaceWind.VariableGust.Template.Text;
        this.VariableGustWindVoiceTemplate = station.AtisFormat.SurfaceWind.VariableGust.Template.Voice;
        this.VariableDirectionWindTextTemplate = station.AtisFormat.SurfaceWind.VariableDirection.Template.Text;
        this.VariableDirectionWindVoiceTemplate = station.AtisFormat.SurfaceWind.VariableDirection.Template.Voice;
        this.CalmWindTextTemplate = station.AtisFormat.SurfaceWind.Calm.Template.Text;
        this.CalmWindVoiceTemplate = station.AtisFormat.SurfaceWind.Calm.Template.Voice;
        this.CalmWindSpeed = station.AtisFormat.SurfaceWind.Calm.CalmWindSpeed.ToString();
        this.VisibilityTextTemplate = station.AtisFormat.Visibility.Template.Text;
        this.VisibilityVoiceTemplate = station.AtisFormat.Visibility.Template.Voice;
        this.PresentWeatherTextTemplate = station.AtisFormat.PresentWeather.Template.Text;
        this.PresentWeatherVoiceTemplate = station.AtisFormat.PresentWeather.Template.Voice;
        this.RecentWeatherVoiceTemplate = station.AtisFormat.RecentWeather.Template.Voice;
        this.RecentWeatherTextTemplate = station.AtisFormat.RecentWeather.Template.Text;
        this.CloudsTextTemplate = station.AtisFormat.Clouds.Template.Text;
        this.CloudsVoiceTemplate = station.AtisFormat.Clouds.Template.Voice;
        this.TemperatureTextTemplate = station.AtisFormat.Temperature.Template.Text;
        this.TemperatureVoiceTemplate = station.AtisFormat.Temperature.Template.Voice;
        this.DewpointTextTemplate = station.AtisFormat.Dewpoint.Template.Text;
        this.DewpointVoiceTemplate = station.AtisFormat.Dewpoint.Template.Voice;
        this.AltimeterTextTemplate = station.AtisFormat.Altimeter.Template.Text;
        this.AltimeterVoiceTemplate = station.AtisFormat.Altimeter.Template.Voice;
        this.ClosingStatementTextTemplate = station.AtisFormat.ClosingStatement.Template.Text;
        this.ClosingStatementVoiceTemplate = station.AtisFormat.ClosingStatement.Template.Voice;
        this.VisibilityNorth = station.AtisFormat.Visibility.North;
        this.VisibilityNorthEast = station.AtisFormat.Visibility.NorthEast;
        this.VisibilityEast = station.AtisFormat.Visibility.East;
        this.VisibilitySouthEast = station.AtisFormat.Visibility.SouthEast;
        this.VisibilitySouth = station.AtisFormat.Visibility.South;
        this.VisibilitySouthWest = station.AtisFormat.Visibility.SouthWest;
        this.VisibilityWest = station.AtisFormat.Visibility.West;
        this.VisibilityNorthWest = station.AtisFormat.Visibility.NorthWest;
        this.VisibilityUnlimitedVisibilityVoice = station.AtisFormat.Visibility.UnlimitedVisibilityVoice;
        this.VisibilityUnlimitedVisibilityText = station.AtisFormat.Visibility.UnlimitedVisibilityText;
        this.VisibilityIncludeVisibilitySuffix = station.AtisFormat.Visibility.IncludeVisibilitySuffix;
        this.VisibilityMetersCutoff = station.AtisFormat.Visibility.MetersCutoff;
        this.PresentWeatherLightIntensity = station.AtisFormat.PresentWeather.LightIntensity;
        this.PresentWeatherModerateIntensity = station.AtisFormat.PresentWeather.ModerateIntensity;
        this.PresentWeatherHeavyIntensity = station.AtisFormat.PresentWeather.HeavyIntensity;
        this.PresentWeatherVicinity = station.AtisFormat.PresentWeather.Vicinity;
        this.CloudsIdentifyCeilingLayer = station.AtisFormat.Clouds.IdentifyCeilingLayer;
        this.CloudsConvertToMetric = station.AtisFormat.Clouds.ConvertToMetric;
        this.CloudHeightAltitudeInHundreds = station.AtisFormat.Clouds.IsAltitudeInHundreds;
        this.UndeterminedLayerAltitudeText = station.AtisFormat.Clouds.UndeterminedLayerAltitude.Text;
        this.UndeterminedLayerAltitudeVoice = station.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice;
        this.TemperatureUsePlusPrefix = station.AtisFormat.Temperature.UsePlusPrefix;
        this.TemperatureSpeakLeadingZero = station.AtisFormat.Temperature.SpeakLeadingZero;
        this.DewpointUsePlusPrefix = station.AtisFormat.Dewpoint.UsePlusPrefix;
        this.DewpointSpeakLeadingZero = station.AtisFormat.Dewpoint.SpeakLeadingZero;
        this.AltimeterSpeakDecimal = station.AtisFormat.Altimeter.PronounceDecimal;
        this.NotamsTextTemplate = station.AtisFormat.Notams.Template.Text;
        this.NotamsVoiceTemplate = station.AtisFormat.Notams.Template.Voice;
        this.ClosingStatementAutoIncludeClosingStatement =
            station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement;

        this.PresentWeatherTypes = [];
        foreach (var kvp in station.AtisFormat.PresentWeather.PresentWeatherTypes)
        {
            this.PresentWeatherTypes.Add(new PresentWeatherMeta(kvp.Key, kvp.Value.Text, kvp.Value.Spoken));
        }

        this.CloudTypes = [];
        foreach (var item in station.AtisFormat.Clouds.Types)
        {
            this.CloudTypes.Add(new CloudTypeMeta(item.Key, item.Value.Voice, item.Value.Text));
        }

        this.ConvectiveCloudTypes = [];
        foreach (var item in station.AtisFormat.Clouds.ConvectiveTypes)
        {
            this.ConvectiveCloudTypes.Add(new ConvectiveCloudTypeMeta(item.Key, item.Value));
        }

        this.TransitionLevelTextTemplate = station.AtisFormat.TransitionLevel.Template.Text;
        this.TransitionLevelVoiceTemplate = station.AtisFormat.TransitionLevel.Template.Voice;

        this.TransitionLevels = [];
        foreach (var item in station.AtisFormat.TransitionLevel.Values.OrderBy(x => x.Low))
        {
            this.TransitionLevels.Add(item);
        }

        this.ContractionCompletionData = [];
        foreach (var contraction in station.Contractions)
        {
            if (!string.IsNullOrEmpty(contraction.VariableName) && !string.IsNullOrEmpty(contraction.Voice))
            {
                this.ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
            }
        }

        this.HasUnsavedChanges = false;
    }

    private async Task HandleAddTransitionLevel()
    {
        if (this.DialogOwner == null || this.SelectedStation == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            string? previousQnhLow = null;
            string? previousQnhHigh = null;
            string? previousTransitionLevel = null;

            var dialog = this._windowFactory.CreateTransitionLevelDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is TransitionLevelDialogViewModel context)
            {
                context.QnhLow = previousQnhLow;
                context.QnhHigh = previousQnhHigh;
                context.TransitionLevel = previousTransitionLevel;

                context.DialogResultChanged += (_, dialogResult) =>
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        context.ClearAllErrors();

                        previousQnhLow = context.QnhLow;
                        previousQnhHigh = context.QnhHigh;
                        previousTransitionLevel = context.TransitionLevel;

                        if (string.IsNullOrEmpty(context.QnhLow))
                        {
                            context.RaiseError("QnhLow", "Value is required.");
                        }

                        if (string.IsNullOrEmpty(context.QnhHigh))
                        {
                            context.RaiseError("QnhHigh", "Value is required.");
                        }

                        if (string.IsNullOrEmpty(context.TransitionLevel))
                        {
                            context.RaiseError("TransitionLevel", "Value is required.");
                        }

                        int.TryParse(context.QnhLow, out var intLow);
                        int.TryParse(context.QnhHigh, out var intHigh);
                        int.TryParse(context.TransitionLevel, out var intLevel);

                        if (this.SelectedStation.AtisFormat.TransitionLevel.Values.Any(
                                x =>
                                    x.Low == intLow && x.High == intHigh))
                        {
                            context.RaiseError("QnhLow", "Duplicate transition level.");
                        }

                        if (context.HasErrors)
                        {
                            return;
                        }

                        this.SelectedStation.AtisFormat.TransitionLevel.Values.Add(
                            new TransitionLevelMeta(intLow, intHigh, intLevel));

                        this.TransitionLevels = [];
                        var sorted = this.SelectedStation.AtisFormat.TransitionLevel.Values.OrderBy(x => x.Low);
                        foreach (var item in sorted)
                        {
                            this.TransitionLevels.Add(item);
                        }

                        if (this._sessionManager.CurrentProfile != null)
                        {
                            this._profileRepository.Save(this._sessionManager.CurrentProfile);
                        }
                    }
                };
                await dialog.ShowDialog((Window)this.DialogOwner);
            }
        }
    }

    private async Task HandleDeleteTransitionLevel(TransitionLevelMeta? obj)
    {
        if (obj == null || this.TransitionLevels == null || this.DialogOwner == null || this.SelectedStation == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)this.DialogOwner,
                "Are you sure you want to delete the selected transition level?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (this.TransitionLevels.Remove(obj))
            {
                this.SelectedStation.AtisFormat.TransitionLevel.Values.Remove(obj);
                if (this._sessionManager.CurrentProfile != null)
                {
                    this._profileRepository.Save(this._sessionManager.CurrentProfile);
                }
            }
        }
    }

    private void HandleCellEditEnding(DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            this.HasUnsavedChanges = true;
        }
    }

    private void HandleTemplateVariableClicked(string? variable)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(lifetime.MainWindow);
            var focusedElement = topLevel?.FocusManager?.GetFocusedElement();

            if (focusedElement is TemplateVariableTextBox focusedTextBox)
            {
                focusedTextBox.Text += variable?.Replace("__", "_");
                focusedTextBox.CaretIndex = focusedTextBox.Text.Length;
            }

            if (focusedElement is TextArea focusedTextEditor)
            {
                focusedTextEditor.Document.Text += variable?.Replace("__", "_");
                focusedTextEditor.Caret.Offset = focusedTextEditor.Document.Text.Length;
            }
        }
    }

    public bool ApplyChanges()
    {
        if (this.SelectedStation == null)
        {
            return true;
        }

        this.ClearAllErrors();

        if (!string.IsNullOrEmpty(this.RoutineObservationTime))
        {
            var observationTimes = new List<int>();
            foreach (var interval in this.RoutineObservationTime.Split(','))
            {
                if (int.TryParse(interval, out var value))
                {
                    if (value < 0 || value > 59)
                    {
                        this.RaiseError(
                            nameof(this.RoutineObservationTime),
                            "Invalid routine observation time. Time values must between 0 and 59.");
                        return false;
                    }

                    if (observationTimes.Contains(value))
                    {
                        this.RaiseError(
                            nameof(this.RoutineObservationTime),
                            "Duplicate routine observation time values.");
                        return false;
                    }

                    observationTimes.Add(value);
                }
            }

            this.SelectedStation.AtisFormat.ObservationTime.StandardUpdateTime = observationTimes;
        }
        else
        {
            this.SelectedStation.AtisFormat.ObservationTime.StandardUpdateTime = null;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.SpeakLeadingZero != this.SpeakWindSpeedLeadingZero)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.SpeakLeadingZero = this.SpeakWindSpeedLeadingZero;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled != this.MagneticVariationEnabled)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled = this.MagneticVariationEnabled;
        }

        if (int.TryParse(this.MagneticVariationValue, out var magneticVariation))
        {
            if (this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees != magneticVariation)
            {
                this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = magneticVariation;
            }
        }
        else
        {
            this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.Enabled = false;
            this.SelectedStation.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = 0;
        }

        if (this.SelectedStation.AtisFormat.ObservationTime.Template.Text != this.ObservationTimeTextTemplate)
        {
            this.SelectedStation.AtisFormat.ObservationTime.Template.Text = this.ObservationTimeTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.ObservationTime.Template.Voice != this.ObservationTimeVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.ObservationTime.Template.Voice = this.ObservationTimeVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Text != this.StandardWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Text = this.StandardWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Voice != this.StandardWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Standard.Template.Voice = this.StandardWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Text != this.StandardGustWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Text = this.StandardGustWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Voice !=
            this.StandardGustWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.StandardGust.Template.Voice =
                this.StandardGustWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Text != this.VariableWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Text = this.VariableWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Voice != this.VariableWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Variable.Template.Voice = this.VariableWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Text != this.VariableGustWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Text = this.VariableGustWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Voice !=
            this.VariableGustWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.VariableGust.Template.Voice =
                this.VariableGustWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Text !=
            this.VariableDirectionWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Text =
                this.VariableDirectionWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Voice !=
            this.VariableDirectionWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.VariableDirection.Template.Voice =
                this.VariableDirectionWindVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Text != this.CalmWindTextTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Text = this.CalmWindTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Voice != this.CalmWindVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.SurfaceWind.Calm.Template.Voice = this.CalmWindVoiceTemplate;
        }

        if (int.TryParse(this.CalmWindSpeed, out var speed))
        {
            if (this.SelectedStation.AtisFormat.SurfaceWind.Calm.CalmWindSpeed != speed)
            {
                this.SelectedStation.AtisFormat.SurfaceWind.Calm.CalmWindSpeed = speed;
            }
        }

        if (this.SelectedStation.AtisFormat.Visibility.Template.Text != this.VisibilityTextTemplate)
        {
            this.SelectedStation.AtisFormat.Visibility.Template.Text = this.VisibilityTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Visibility.Template.Voice != this.VisibilityVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Visibility.Template.Voice = this.VisibilityVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.Template.Text != this.PresentWeatherTextTemplate)
        {
            this.SelectedStation.AtisFormat.PresentWeather.Template.Text = this.PresentWeatherTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.Template.Voice != this.PresentWeatherVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.PresentWeather.Template.Voice = this.PresentWeatherVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.RecentWeather.Template.Text != this.RecentWeatherTextTemplate)
        {
            this.SelectedStation.AtisFormat.RecentWeather.Template.Text = this.RecentWeatherTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.RecentWeather.Template.Voice != this.RecentWeatherVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.RecentWeather.Template.Voice = this.RecentWeatherVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Clouds.Template.Text != this.CloudsTextTemplate)
        {
            this.SelectedStation.AtisFormat.Clouds.Template.Text = this.CloudsTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Clouds.Template.Voice != this.CloudsVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Clouds.Template.Voice = this.CloudsVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Temperature.Template.Text != this.TemperatureTextTemplate)
        {
            this.SelectedStation.AtisFormat.Temperature.Template.Text = this.TemperatureTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Temperature.Template.Voice != this.TemperatureVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Temperature.Template.Voice = this.TemperatureVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Dewpoint.Template.Text != this.DewpointTextTemplate)
        {
            this.SelectedStation.AtisFormat.Dewpoint.Template.Text = this.DewpointTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Dewpoint.Template.Voice != this.DewpointVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Dewpoint.Template.Voice = this.DewpointVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Altimeter.Template.Text != this.AltimeterTextTemplate)
        {
            this.SelectedStation.AtisFormat.Altimeter.Template.Text = this.AltimeterTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Altimeter.Template.Voice != this.AltimeterVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Altimeter.Template.Voice = this.AltimeterVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.ClosingStatement.Template.Text != this.ClosingStatementTextTemplate)
        {
            this.SelectedStation.AtisFormat.ClosingStatement.Template.Text = this.ClosingStatementTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.ClosingStatement.Template.Voice != this.ClosingStatementVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.ClosingStatement.Template.Voice = this.ClosingStatementVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Visibility.North != this.VisibilityNorth)
        {
            this.SelectedStation.AtisFormat.Visibility.North = this.VisibilityNorth ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.NorthEast != this.VisibilityNorthEast)
        {
            this.SelectedStation.AtisFormat.Visibility.NorthEast = this.VisibilityNorthEast ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.East != this.VisibilityEast)
        {
            this.SelectedStation.AtisFormat.Visibility.East = this.VisibilityEast ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.SouthEast != this.VisibilitySouthEast)
        {
            this.SelectedStation.AtisFormat.Visibility.SouthEast = this.VisibilitySouthEast ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.South != this.VisibilitySouth)
        {
            this.SelectedStation.AtisFormat.Visibility.South = this.VisibilitySouth ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.SouthWest != this.VisibilitySouthWest)
        {
            this.SelectedStation.AtisFormat.Visibility.SouthWest = this.VisibilitySouthWest ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.West != this.VisibilityWest)
        {
            this.SelectedStation.AtisFormat.Visibility.West = this.VisibilityWest ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.NorthWest != this.VisibilityNorthWest)
        {
            this.SelectedStation.AtisFormat.Visibility.NorthWest = this.VisibilityNorthWest ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityVoice !=
            this.VisibilityUnlimitedVisibilityVoice)
        {
            this.SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityVoice =
                this.VisibilityUnlimitedVisibilityVoice ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityText !=
            this.VisibilityUnlimitedVisibilityText)
        {
            this.SelectedStation.AtisFormat.Visibility.UnlimitedVisibilityText =
                this.VisibilityUnlimitedVisibilityText ?? "";
        }

        if (this.SelectedStation.AtisFormat.Visibility.IncludeVisibilitySuffix !=
            this.VisibilityIncludeVisibilitySuffix)
        {
            this.SelectedStation.AtisFormat.Visibility.IncludeVisibilitySuffix = this.VisibilityIncludeVisibilitySuffix;
        }

        if (this.SelectedStation.AtisFormat.Visibility.MetersCutoff != this.VisibilityMetersCutoff)
        {
            this.SelectedStation.AtisFormat.Visibility.MetersCutoff = this.VisibilityMetersCutoff;
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.LightIntensity != this.PresentWeatherLightIntensity)
        {
            this.SelectedStation.AtisFormat.PresentWeather.LightIntensity = this.PresentWeatherLightIntensity ?? "";
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.ModerateIntensity != this.PresentWeatherModerateIntensity)
        {
            this.SelectedStation.AtisFormat.PresentWeather.ModerateIntensity =
                this.PresentWeatherModerateIntensity ?? "";
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.HeavyIntensity != this.PresentWeatherHeavyIntensity)
        {
            this.SelectedStation.AtisFormat.PresentWeather.HeavyIntensity = this.PresentWeatherHeavyIntensity ?? "";
        }

        if (this.SelectedStation.AtisFormat.PresentWeather.Vicinity != this.PresentWeatherVicinity)
        {
            this.SelectedStation.AtisFormat.PresentWeather.Vicinity = this.PresentWeatherVicinity ?? "";
        }

        if (this.PresentWeatherTypes != null && this.SelectedStation.AtisFormat.PresentWeather.PresentWeatherTypes !=
            this.PresentWeatherTypes.ToDictionary(
                x => x.Key,
                x => new PresentWeather.WeatherDescriptorType(x.Text, x.Spoken)))
        {
            this.SelectedStation.AtisFormat.PresentWeather.PresentWeatherTypes = this.PresentWeatherTypes.ToDictionary(
                x => x.Key,
                x => new PresentWeather.WeatherDescriptorType(x.Text, x.Spoken));
        }

        if (this.SelectedStation.AtisFormat.Clouds.IdentifyCeilingLayer != this.CloudsIdentifyCeilingLayer)
        {
            this.SelectedStation.AtisFormat.Clouds.IdentifyCeilingLayer = this.CloudsIdentifyCeilingLayer;
        }

        if (this.SelectedStation.AtisFormat.Clouds.ConvertToMetric != this.CloudsConvertToMetric)
        {
            this.SelectedStation.AtisFormat.Clouds.ConvertToMetric = this.CloudsConvertToMetric;
        }

        if (this.SelectedStation.AtisFormat.Clouds.IsAltitudeInHundreds != this.CloudHeightAltitudeInHundreds)
        {
            this.SelectedStation.AtisFormat.Clouds.IsAltitudeInHundreds = this.CloudHeightAltitudeInHundreds;
        }

        if (this.SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Text != this.UndeterminedLayerAltitudeText)
        {
            this.SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Text =
                this.UndeterminedLayerAltitudeText ?? "";
        }

        if (this.SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice !=
            this.UndeterminedLayerAltitudeVoice)
        {
            this.SelectedStation.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice =
                this.UndeterminedLayerAltitudeVoice ?? "";
        }

        if (this.CloudTypes != null && this.SelectedStation.AtisFormat.Clouds.Types != this.CloudTypes.ToDictionary(
                x => x.Acronym,
                meta => new CloudType(meta.Text, meta.Spoken)))
        {
            this.SelectedStation.AtisFormat.Clouds.Types = this.CloudTypes.ToDictionary(
                x => x.Acronym,
                meta => new CloudType(meta.Text, meta.Spoken));
        }

        if (this.ConvectiveCloudTypes != null && this.SelectedStation.AtisFormat.Clouds.ConvectiveTypes !=
            this.ConvectiveCloudTypes.ToDictionary(x => x.Key, x => x.Value))
        {
            this.SelectedStation.AtisFormat.Clouds.ConvectiveTypes =
                this.ConvectiveCloudTypes.ToDictionary(x => x.Key, x => x.Value);
        }

        if (this.SelectedStation.AtisFormat.Temperature.UsePlusPrefix != this.TemperatureUsePlusPrefix)
        {
            this.SelectedStation.AtisFormat.Temperature.UsePlusPrefix = this.TemperatureUsePlusPrefix;
        }

        if (this.SelectedStation.AtisFormat.Temperature.SpeakLeadingZero != this.TemperatureSpeakLeadingZero)
        {
            this.SelectedStation.AtisFormat.Temperature.SpeakLeadingZero = this.TemperatureSpeakLeadingZero;
        }

        if (this.SelectedStation.AtisFormat.Dewpoint.UsePlusPrefix != this.DewpointUsePlusPrefix)
        {
            this.SelectedStation.AtisFormat.Dewpoint.UsePlusPrefix = this.DewpointUsePlusPrefix;
        }

        if (this.SelectedStation.AtisFormat.Dewpoint.SpeakLeadingZero != this.DewpointSpeakLeadingZero)
        {
            this.SelectedStation.AtisFormat.Dewpoint.SpeakLeadingZero = this.DewpointSpeakLeadingZero;
        }

        if (this.SelectedStation.AtisFormat.Altimeter.PronounceDecimal != this.AltimeterSpeakDecimal)
        {
            this.SelectedStation.AtisFormat.Altimeter.PronounceDecimal = this.AltimeterSpeakDecimal;
        }

        if (this.SelectedStation.AtisFormat.ClosingStatement.AutoIncludeClosingStatement !=
            this.ClosingStatementAutoIncludeClosingStatement)
        {
            this.SelectedStation.AtisFormat.ClosingStatement.AutoIncludeClosingStatement =
                this.ClosingStatementAutoIncludeClosingStatement;
        }

        if (this.SelectedStation.AtisFormat.TransitionLevel.Template.Text != this.TransitionLevelTextTemplate)
        {
            this.SelectedStation.AtisFormat.TransitionLevel.Template.Text = this.TransitionLevelTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.TransitionLevel.Template.Voice != this.TransitionLevelVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.TransitionLevel.Template.Voice = this.TransitionLevelVoiceTemplate;
        }

        if (this.SelectedStation.AtisFormat.Notams.Template.Text != this.NotamsTextTemplate)
        {
            this.SelectedStation.AtisFormat.Notams.Template.Text = this.NotamsTextTemplate;
        }

        if (this.SelectedStation.AtisFormat.Notams.Template.Voice != this.NotamsVoiceTemplate)
        {
            this.SelectedStation.AtisFormat.Notams.Template.Voice = this.NotamsVoiceTemplate;
        }

        if (this.HasErrors)
        {
            return false;
        }

        if (this._sessionManager.CurrentProfile != null)
        {
            this._profileRepository.Save(this._sessionManager.CurrentProfile);
        }

        this.HasUnsavedChanges = false;

        return true;
    }

    #region UI Properties

    private ObservableCollection<string>? _formattingOptions;

    public ObservableCollection<string>? FormattingOptions
    {
        get => this._formattingOptions;
        set => this.RaiseAndSetIfChanged(ref this._formattingOptions, value);
    }

    private AtisStation? _selectedStation;

    public AtisStation? SelectedStation
    {
        get => this._selectedStation;
        private set => this.RaiseAndSetIfChanged(ref this._selectedStation, value);
    }

    private bool _hasUnsavedChanges;

    public bool HasUnsavedChanges
    {
        get => this._hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref this._hasUnsavedChanges, value);
    }

    private string? _selectedFormattingOption;

    public string? SelectedFormattingOption
    {
        get => this._selectedFormattingOption;
        set => this.RaiseAndSetIfChanged(ref this._selectedFormattingOption, value);
    }

    #endregion

    #region Config Properties

    private string? _routineObservationTime;

    public string? RoutineObservationTime
    {
        get => this._routineObservationTime;
        set
        {
            this.RaiseAndSetIfChanged(ref this._routineObservationTime, value);
            if (!this._initializedProperties.Add(nameof(this.RoutineObservationTime)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _observationTimeTextTemplate;

    public string? ObservationTimeTextTemplate
    {
        get => this._observationTimeTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._observationTimeTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.ObservationTimeTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _observationTimeVoiceTemplate;

    public string? ObservationTimeVoiceTemplate
    {
        get => this._observationTimeVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._observationTimeVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.ObservationTimeVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _speakWindSpeedLeadingZero;

    public bool SpeakWindSpeedLeadingZero
    {
        get => this._speakWindSpeedLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref this._speakWindSpeedLeadingZero, value);
            if (!this._initializedProperties.Add(nameof(this.SpeakWindSpeedLeadingZero)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _magneticVariationEnabled;

    public bool MagneticVariationEnabled
    {
        get => this._magneticVariationEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref this._magneticVariationEnabled, value);
            if (!this._initializedProperties.Add(nameof(this.MagneticVariationEnabled)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _magneticVariationValue;

    public string? MagneticVariationValue
    {
        get => this._magneticVariationValue;
        set
        {
            this.RaiseAndSetIfChanged(ref this._magneticVariationValue, value);
            if (!this._initializedProperties.Add(nameof(this.MagneticVariationValue)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _standardWindTextTemplate;

    public string? StandardWindTextTemplate
    {
        get => this._standardWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._standardWindTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.StandardWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _standardWindVoiceTemplate;

    public string? StandardWindVoiceTemplate
    {
        get => this._standardWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._standardWindVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.StandardWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _standardGustWindTextTemplate;

    public string? StandardGustWindTextTemplate
    {
        get => this._standardGustWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._standardGustWindTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.StandardGustWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _standardGustWindVoiceTemplate;

    public string? StandardGustWindVoiceTemplate
    {
        get => this._standardGustWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._standardGustWindVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.StandardGustWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableWindTextTemplate;

    public string? VariableWindTextTemplate
    {
        get => this._variableWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._variableWindTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.VariableWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableWindVoiceTemplate;

    public string? VariableWindVoiceTemplate
    {
        get => this._variableWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._variableWindVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.VariableWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableGustWindTextTemplate;

    public string? VariableGustWindTextTemplate
    {
        get => this._variableGustWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._variableGustWindTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.VariableGustWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableGustWindVoiceTemplate;

    public string? VariableGustWindVoiceTemplate
    {
        get => this._variableGustWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._variableGustWindVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.VariableGustWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableDirectionWindTextTemplate;

    public string? VariableDirectionWindTextTemplate
    {
        get => this._variableDirectionWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._variableDirectionWindTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.VariableDirectionWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _variableDirectionWindVoiceTemplate;

    public string? VariableDirectionWindVoiceTemplate
    {
        get => this._variableDirectionWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._variableDirectionWindVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.VariableDirectionWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _calmWindTextTemplate;

    public string? CalmWindTextTemplate
    {
        get => this._calmWindTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._calmWindTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.CalmWindTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _calmWindVoiceTemplate;

    public string? CalmWindVoiceTemplate
    {
        get => this._calmWindVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._calmWindVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.CalmWindVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _calmWindSpeed;

    public string? CalmWindSpeed
    {
        get => this._calmWindSpeed;
        set
        {
            this.RaiseAndSetIfChanged(ref this._calmWindSpeed, value);
            if (!this._initializedProperties.Add(nameof(this.CalmWindSpeed)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityTextTemplate;

    public string? VisibilityTextTemplate
    {
        get => this._visibilityTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityVoiceTemplate;

    public string? VisibilityVoiceTemplate
    {
        get => this._visibilityVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherTextTemplate;

    public string? PresentWeatherTextTemplate
    {
        get => this._presentWeatherTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._presentWeatherTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.PresentWeatherTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherVoiceTemplate;

    public string? PresentWeatherVoiceTemplate
    {
        get => this._presentWeatherVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._presentWeatherVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.PresentWeatherVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _recentWeatherTextTemplate;

    public string? RecentWeatherTextTemplate
    {
        get => this._recentWeatherTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._recentWeatherTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.RecentWeatherTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _recentWeatherVoiceTemplate;

    public string? RecentWeatherVoiceTemplate
    {
        get => this._recentWeatherVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._recentWeatherVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.RecentWeatherVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _cloudsTextTemplate;

    public string? CloudsTextTemplate
    {
        get => this._cloudsTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._cloudsTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.CloudsTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _cloudsVoiceTemplate;

    public string? CloudsVoiceTemplate
    {
        get => this._cloudsVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._cloudsVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.CloudsVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _temperatureTextTemplate;

    public string? TemperatureTextTemplate
    {
        get => this._temperatureTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._temperatureTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.TemperatureTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _temperatureVoiceTemplate;

    public string? TemperatureVoiceTemplate
    {
        get => this._temperatureVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._temperatureVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.TemperatureVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _dewpointTextTemplate;

    public string? DewpointTextTemplate
    {
        get => this._dewpointTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._dewpointTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.DewpointTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _dewpointVoiceTemplate;

    public string? DewpointVoiceTemplate
    {
        get => this._dewpointVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._dewpointVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.DewpointVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _altimeterTextTemplate;

    public string? AltimeterTextTemplate
    {
        get => this._altimeterTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._altimeterTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.AltimeterTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _altimeterVoiceTemplate;

    public string? AltimeterVoiceTemplate
    {
        get => this._altimeterVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._altimeterVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.AltimeterVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _closingStatementTextTemplate;

    public string? ClosingStatementTextTemplate
    {
        get => this._closingStatementTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._closingStatementTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.ClosingStatementTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _closingStatementVoiceTemplate;

    public string? ClosingStatementVoiceTemplate
    {
        get => this._closingStatementVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._closingStatementVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.ClosingStatementVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _notamsTextTemplate;

    public string? NotamsTextTemplate
    {
        get => this._notamsTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._notamsTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.NotamsTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _notamsVoiceTemplate;

    public string? NotamsVoiceTemplate
    {
        get => this._notamsVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._notamsVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.NotamsVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityNorth;

    public string? VisibilityNorth
    {
        get => this._visibilityNorth;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityNorth, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityNorth)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityNorthEast;

    public string? VisibilityNorthEast
    {
        get => this._visibilityNorthEast;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityNorthEast, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityNorthEast)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityEast;

    public string? VisibilityEast
    {
        get => this._visibilityEast;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityEast, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityEast)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilitySouthEast;

    public string? VisibilitySouthEast
    {
        get => this._visibilitySouthEast;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilitySouthEast, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilitySouthEast)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilitySouth;

    public string? VisibilitySouth
    {
        get => this._visibilitySouth;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilitySouth, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilitySouth)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilitySouthWest;

    public string? VisibilitySouthWest
    {
        get => this._visibilitySouthWest;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilitySouthWest, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilitySouthWest)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityWest;

    public string? VisibilityWest
    {
        get => this._visibilityWest;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityWest, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityWest)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityNorthWest;

    public string? VisibilityNorthWest
    {
        get => this._visibilityNorthWest;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityNorthWest, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityNorthWest)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityUnlimitedVisibilityVoice;

    public string? VisibilityUnlimitedVisibilityVoice
    {
        get => this._visibilityUnlimitedVisibilityVoice;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityUnlimitedVisibilityVoice, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityUnlimitedVisibilityVoice)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _visibilityUnlimitedVisibilityText;

    public string? VisibilityUnlimitedVisibilityText
    {
        get => this._visibilityUnlimitedVisibilityText;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityUnlimitedVisibilityText, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityUnlimitedVisibilityText)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _visibilityIncludeVisibilitySuffix;

    public bool VisibilityIncludeVisibilitySuffix
    {
        get => this._visibilityIncludeVisibilitySuffix;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityIncludeVisibilitySuffix, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityIncludeVisibilitySuffix)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private int _visibilityMetersCutoff;

    public int VisibilityMetersCutoff
    {
        get => this._visibilityMetersCutoff;
        set
        {
            this.RaiseAndSetIfChanged(ref this._visibilityMetersCutoff, value);
            if (!this._initializedProperties.Add(nameof(this.VisibilityMetersCutoff)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherLightIntensity;

    public string? PresentWeatherLightIntensity
    {
        get => this._presentWeatherLightIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref this._presentWeatherLightIntensity, value);
            if (!this._initializedProperties.Add(nameof(this.PresentWeatherLightIntensity)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherModerateIntensity;

    public string? PresentWeatherModerateIntensity
    {
        get => this._presentWeatherModerateIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref this._presentWeatherModerateIntensity, value);
            if (!this._initializedProperties.Add(nameof(this.PresentWeatherModerateIntensity)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherHeavyIntensity;

    public string? PresentWeatherHeavyIntensity
    {
        get => this._presentWeatherHeavyIntensity;
        set
        {
            this.RaiseAndSetIfChanged(ref this._presentWeatherHeavyIntensity, value);
            if (!this._initializedProperties.Add(nameof(this.PresentWeatherHeavyIntensity)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _presentWeatherVicinity;

    public string? PresentWeatherVicinity
    {
        get => this._presentWeatherVicinity;
        set
        {
            this.RaiseAndSetIfChanged(ref this._presentWeatherVicinity, value);
            if (!this._initializedProperties.Add(nameof(this.PresentWeatherVicinity)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _cloudsIdentifyCeilingLayer;

    public bool CloudsIdentifyCeilingLayer
    {
        get => this._cloudsIdentifyCeilingLayer;
        set
        {
            this.RaiseAndSetIfChanged(ref this._cloudsIdentifyCeilingLayer, value);
            if (!this._initializedProperties.Add(nameof(this.CloudsIdentifyCeilingLayer)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _cloudsConvertToMetric;

    public bool CloudsConvertToMetric
    {
        get => this._cloudsConvertToMetric;
        set
        {
            this.RaiseAndSetIfChanged(ref this._cloudsConvertToMetric, value);
            if (!this._initializedProperties.Add(nameof(this.CloudsConvertToMetric)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _undeterminedLayerAltitudeText;

    public string? UndeterminedLayerAltitudeText
    {
        get => this._undeterminedLayerAltitudeText;
        set
        {
            this.RaiseAndSetIfChanged(ref this._undeterminedLayerAltitudeText, value);
            if (!this._initializedProperties.Add(nameof(this.UndeterminedLayerAltitudeText)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _undeterminedLayerAltitudeVoice;

    public string? UndeterminedLayerAltitudeVoice
    {
        get => this._undeterminedLayerAltitudeVoice;
        set
        {
            this.RaiseAndSetIfChanged(ref this._undeterminedLayerAltitudeVoice, value);
            if (!this._initializedProperties.Add(nameof(this.UndeterminedLayerAltitudeVoice)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _cloudHeightAltitudeInHundreds;

    public bool CloudHeightAltitudeInHundreds
    {
        get => this._cloudHeightAltitudeInHundreds;
        set
        {
            this.RaiseAndSetIfChanged(ref this._cloudHeightAltitudeInHundreds, value);
            if (!this._initializedProperties.Add(nameof(this.CloudHeightAltitudeInHundreds)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _temperatureUsePlusPrefix;

    public bool TemperatureUsePlusPrefix
    {
        get => this._temperatureUsePlusPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref this._temperatureUsePlusPrefix, value);
            if (!this._initializedProperties.Add(nameof(this.TemperatureUsePlusPrefix)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _temperatureSpeakLeadingZero;

    public bool TemperatureSpeakLeadingZero
    {
        get => this._temperatureSpeakLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref this._temperatureSpeakLeadingZero, value);
            if (!this._initializedProperties.Add(nameof(this.TemperatureSpeakLeadingZero)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _dewpointUsePlusPrefix;

    public bool DewpointUsePlusPrefix
    {
        get => this._dewpointUsePlusPrefix;
        set
        {
            this.RaiseAndSetIfChanged(ref this._dewpointUsePlusPrefix, value);
            if (!this._initializedProperties.Add(nameof(this.DewpointUsePlusPrefix)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _dewpointSpeakLeadingZero;

    public bool DewpointSpeakLeadingZero
    {
        get => this._dewpointSpeakLeadingZero;
        set
        {
            this.RaiseAndSetIfChanged(ref this._dewpointSpeakLeadingZero, value);
            if (!this._initializedProperties.Add(nameof(this.DewpointSpeakLeadingZero)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _altimeterSpeakDecimal;

    private bool AltimeterSpeakDecimal
    {
        get => this._altimeterSpeakDecimal;
        set
        {
            this.RaiseAndSetIfChanged(ref this._altimeterSpeakDecimal, value);
            if (!this._initializedProperties.Add(nameof(this.AltimeterSpeakDecimal)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private bool _closingStatementAutoIncludeClosingStatement;

    public bool ClosingStatementAutoIncludeClosingStatement
    {
        get => this._closingStatementAutoIncludeClosingStatement;
        set
        {
            this.RaiseAndSetIfChanged(ref this._closingStatementAutoIncludeClosingStatement, value);
            if (!this._initializedProperties.Add(nameof(this.ClosingStatementAutoIncludeClosingStatement)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private ObservableCollection<TransitionLevelMeta>? _transitionLevelMetas;

    public ObservableCollection<TransitionLevelMeta>? TransitionLevels
    {
        get => this._transitionLevelMetas;
        set
        {
            this.RaiseAndSetIfChanged(ref this._transitionLevelMetas, value);
            if (!this._initializedProperties.Add(nameof(this.TransitionLevels)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _transitionLevelTextTemplate;

    public string? TransitionLevelTextTemplate
    {
        get => this._transitionLevelTextTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._transitionLevelTextTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.TransitionLevelTextTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _transitionLevelVoiceTemplate;

    public string? TransitionLevelVoiceTemplate
    {
        get => this._transitionLevelVoiceTemplate;
        set
        {
            this.RaiseAndSetIfChanged(ref this._transitionLevelVoiceTemplate, value);
            if (!this._initializedProperties.Add(nameof(this.TransitionLevelVoiceTemplate)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private ObservableCollection<PresentWeatherMeta>? _presentWeatherTypes;

    public ObservableCollection<PresentWeatherMeta>? PresentWeatherTypes
    {
        get => this._presentWeatherTypes;
        set => this.RaiseAndSetIfChanged(ref this._presentWeatherTypes, value);
    }

    private ObservableCollection<CloudTypeMeta>? _cloudTypes;

    public ObservableCollection<CloudTypeMeta>? CloudTypes
    {
        get => this._cloudTypes;
        set => this.RaiseAndSetIfChanged(ref this._cloudTypes, value);
    }

    private ObservableCollection<ConvectiveCloudTypeMeta>? _convectiveCloudTypes;

    public ObservableCollection<ConvectiveCloudTypeMeta>? ConvectiveCloudTypes
    {
        get => this._convectiveCloudTypes;
        set => this.RaiseAndSetIfChanged(ref this._convectiveCloudTypes, value);
    }

    private List<ICompletionData> _contractionCompletionData = [];

    private List<ICompletionData> ContractionCompletionData
    {
        get => this._contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this._contractionCompletionData, value);
    }

    #endregion
}