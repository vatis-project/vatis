using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Ui.Windows;
using Vatsim.Vatis.Voice.Audio;

namespace Vatsim.Vatis.Ui.ViewModels;

public class VoiceRecordAtisDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly Timer _elapsedTimeUpdateTimer;
    private readonly Timer _maxRecordingDurationTimer;
    private readonly Stopwatch _recordingStopwatch;
    private readonly IWindowLocationService _windowLocationService;

    public VoiceRecordAtisDialogViewModel(
        IAppConfig appConfig,
        IWindowLocationService windowLocationService)
    {
        this._windowLocationService = windowLocationService;
        this._recordingStopwatch = new Stopwatch();
        this._elapsedTimeUpdateTimer = new Timer();
        this._elapsedTimeUpdateTimer.Interval = 50;
        this._elapsedTimeUpdateTimer.Elapsed += (_, _) =>
        {
            this.ElapsedRecordingTime = this._recordingStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            this.ElapsedTime = this._recordingStopwatch.Elapsed;
        };

        this._maxRecordingDurationTimer = new Timer();
        this._maxRecordingDurationTimer.Interval = 180000; // 3 minutes
        this._maxRecordingDurationTimer.AutoReset = false;
        this._maxRecordingDurationTimer.Elapsed += (sender, args) =>
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    this.HandleStopRecordingCommand();

                    ArgumentNullException.ThrowIfNull(this.DialogOwner);

                    _ = MessageBox.ShowDialog(
                        this.DialogOwner,
                        "Maximum ATIS recording duration reached (3 minutes). Recording stopped.",
                        "Warning",
                        MessageBoxButton.Ok,
                        MessageBoxIcon.Information);
                });
        };

        this.SaveCommand = ReactiveCommand.Create<ICloseable>(
            this.HandleSaveCommand,
            this.WhenAnyValue(
                x => x.AudioBuffer,
                x => x.ElapsedTime,
                (buffer, elapsed) => buffer.Length > 0 && elapsed >= TimeSpan.FromSeconds(5)));
        this.CancelCommand = ReactiveCommand.Create<ICloseable>(this.HandleCancelCommand);
        this.StartRecordingCommand = ReactiveCommand.Create(
            this.HandleStartRecordingCommand,
            this.WhenAnyValue(
                x => x.SelectedCaptureDevice,
                x => x.SelectedPlaybackDevice,
                x => x.IsPlaybackActive,
                x => x.IsRecordingActive,
                (capture, playback, playbackActive, recordingActive) =>
                    !string.IsNullOrEmpty(capture) && !string.IsNullOrEmpty(playback) && !playbackActive &&
                    !recordingActive));
        this.StopRecordingCommand = ReactiveCommand.Create(
            this.HandleStopRecordingCommand,
            this.WhenAnyValue(
                x => x.IsPlaybackActive,
                x => x.IsRecordingActive,
                (playbackActive, recordingActive) => !playbackActive && recordingActive));
        this.ListenCommand = ReactiveCommand.Create(
            this.HandleListenCommand,
            this.WhenAnyValue(
                x => x.IsRecordingActive,
                x => x.AudioBuffer,
                (recordingActive, audioBuffer) => !recordingActive && audioBuffer.Length > 0));

        this._disposables.Add(this.CancelCommand);
        this._disposables.Add(this.SaveCommand);
        this._disposables.Add(this.StartRecordingCommand);
        this._disposables.Add(this.StopRecordingCommand);
        this._disposables.Add(this.ListenCommand);

        NativeAudio.GetCaptureDevices(
            (idPtr, namePtr, _) =>
            {
                var id = Marshal.PtrToStringAnsi(idPtr);
                if (id != null)
                {
                    var name = Marshal.PtrToStringAnsi(namePtr);
                    if (name != null)
                    {
                        if (name == appConfig.MicrophoneDevice)
                        {
                            this.SelectedCaptureDevice = name;
                        }

                        this.CaptureDevices.Add(name);
                    }
                }
            });

        NativeAudio.GetPlaybackDevices(
            (idPtr, namePtr, _) =>
            {
                var id = Marshal.PtrToStringAnsi(idPtr);
                if (id != null)
                {
                    var name = Marshal.PtrToStringAnsi(namePtr);
                    if (name != null)
                    {
                        if (name == appConfig.PlaybackDevice)
                        {
                            this.SelectedPlaybackDevice = name;
                        }

                        this.PlaybackDevices.Add(name);
                    }
                }
            });

        this.WhenAnyValue(x => x.SelectedPlaybackDevice).Subscribe(
            _ =>
            {
                appConfig.PlaybackDevice = this.SelectedPlaybackDevice;
                appConfig.SaveConfig();
                NativeAudio.DestroyDevices();
            });

        this.WhenAnyValue(x => x.SelectedCaptureDevice).Subscribe(
            _ =>
            {
                appConfig.MicrophoneDevice = this.SelectedCaptureDevice;
                appConfig.SaveConfig();
                NativeAudio.DestroyDevices();
            });

        this.WhenAnyValue(
            x => x.SelectedCaptureDevice,
            x => x.SelectedPlaybackDevice,
            (capture, playback) =>
                !string.IsNullOrWhiteSpace(capture) &&
                !string.IsNullOrWhiteSpace(playback)).Subscribe(x => this.IsRecordingEnabled = x);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                this.ShowOverlay = lifetime.Windows.Count(w => w.GetType() != typeof(MainWindow)) > 1;
            };
        }
    }

    public ReactiveCommand<ICloseable, Unit> CancelCommand { get; }

    public ReactiveCommand<ICloseable, Unit> SaveCommand { get; }

    public ReactiveCommand<Unit, Unit> StartRecordingCommand { get; }

    public ReactiveCommand<Unit, Unit> StopRecordingCommand { get; }

    public ReactiveCommand<Unit, Unit> ListenCommand { get; }

    public Window? DialogOwner { get; set; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this._disposables.Dispose();
    }

    private void HandleSaveCommand(ICloseable window)
    {
        if (NativeAudio.StopPlayback())
        {
            window.Close(true);
        }
    }

    private void HandleStartRecordingCommand()
    {
        if (this.SelectedCaptureDevice == null)
        {
            return;
        }

        if (NativeAudio.StartRecording(this.SelectedCaptureDevice))
        {
            this.AudioBuffer = [];
            this.IsRecordingEnabled = false;
            this.IsRecordingActive = true;
            this.IsPlaybackEnabled = false;

            this.ElapsedRecordingTime = "00:00:00";
            this._elapsedTimeUpdateTimer.Start();
            this._recordingStopwatch.Start();
            this._maxRecordingDurationTimer.Start();
        }
    }

    private void HandleStopRecordingCommand()
    {
        var temp = new List<byte[]>();
        NativeAudio.StopRecording(
            (data, dataSize) =>
            {
                if (data == IntPtr.Zero)
                {
                    return;
                }

                var buffer = new byte[dataSize];
                Marshal.Copy(data, buffer, 0, dataSize);
                temp.Add(buffer);
            });
        this.AudioBuffer = CombineAudioBuffers(temp);
        this.IsRecordingEnabled = true;
        this.IsRecordingActive = false;
        this.IsPlaybackEnabled = true;

        this._recordingStopwatch.Reset();
        this._elapsedTimeUpdateTimer.Stop();
        this._maxRecordingDurationTimer.Stop();
    }

    private void HandleListenCommand()
    {
        if (this.SelectedPlaybackDevice == null)
        {
            return;
        }

        if (this.IsPlaybackActive)
        {
            if (NativeAudio.StopPlayback())
            {
                this.IsRecordingEnabled = true;
                this.IsPlaybackActive = false;
            }

            return;
        }

        if (NativeAudio.StartPlayback(this.SelectedPlaybackDevice))
        {
            this.IsPlaybackActive = true;
            this.IsRecordingEnabled = false;
        }
    }

    private void HandleCancelCommand(ICloseable window)
    {
        if (NativeAudio.StopPlayback())
        {
            window.Close(false);
        }
    }

    private void UpdateDeviceSelectionEnabled()
    {
        this.DeviceSelectionEnabled = !this.IsPlaybackActive && !this.IsRecordingActive;
    }

    private static byte[] CombineAudioBuffers(List<byte[]> audioBuffers)
    {
        // Calculate the total length of the resulting byte array
        var totalLength = audioBuffers.Sum(byteArray => byteArray.Length);

        // Create a new byte array to hold the combined data
        var result = new byte[totalLength];
        var currentIndex = 0;

        // Copy each byte array into the result array
        foreach (var byteArray in audioBuffers)
        {
            Buffer.BlockCopy(byteArray, 0, result, currentIndex, byteArray.Length);
            currentIndex += byteArray.Length;
        }

        return result;
    }

    public void UpdatePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this._windowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this._windowLocationService.Restore(window);
    }

    #region Reactive Properties

    private bool _showOverlay;

    public bool ShowOverlay
    {
        get => this._showOverlay;
        set => this.RaiseAndSetIfChanged(ref this._showOverlay, value);
    }

    private byte[] _audioBuffer = [];

    public byte[] AudioBuffer
    {
        get => this._audioBuffer;
        private set => this.RaiseAndSetIfChanged(ref this._audioBuffer, value);
    }

    private string? _atisScript;

    public string? AtisScript
    {
        get => this._atisScript;
        set => this.RaiseAndSetIfChanged(ref this._atisScript, value);
    }

    private bool _isPlaybackEnabled;

    public bool IsPlaybackEnabled
    {
        get => this._isPlaybackEnabled;
        set => this.RaiseAndSetIfChanged(ref this._isPlaybackEnabled, value);
    }

    private bool _isPlaybackActive;

    public bool IsPlaybackActive
    {
        get => this._isPlaybackActive;
        set
        {
            this.RaiseAndSetIfChanged(ref this._isPlaybackActive, value);
            this.UpdateDeviceSelectionEnabled();
        }
    }

    private bool _isRecordingActive;

    private bool IsRecordingActive
    {
        get => this._isRecordingActive;
        set
        {
            this.RaiseAndSetIfChanged(ref this._isRecordingActive, value);
            this.UpdateDeviceSelectionEnabled();
        }
    }

    private bool _isRecordingEnabled;

    public bool IsRecordingEnabled
    {
        get => this._isRecordingEnabled;
        set => this.RaiseAndSetIfChanged(ref this._isRecordingEnabled, value);
    }

    private bool _deviceSelectionEnabled = true;

    public bool DeviceSelectionEnabled
    {
        get => this._deviceSelectionEnabled;
        set => this.RaiseAndSetIfChanged(ref this._deviceSelectionEnabled, value);
    }

    private ObservableCollection<string> _captureDevices = [];

    public ObservableCollection<string> CaptureDevices
    {
        get => this._captureDevices;
        set => this.RaiseAndSetIfChanged(ref this._captureDevices, value);
    }

    private ObservableCollection<string> _playbackDevices = [];

    public ObservableCollection<string> PlaybackDevices
    {
        get => this._playbackDevices;
        set => this.RaiseAndSetIfChanged(ref this._playbackDevices, value);
    }

    private string? _selectedCaptureDevice;

    public string? SelectedCaptureDevice
    {
        get => this._selectedCaptureDevice;
        set => this.RaiseAndSetIfChanged(ref this._selectedCaptureDevice, value);
    }

    private string? _selectedPlaybackDevice;

    public string? SelectedPlaybackDevice
    {
        get => this._selectedPlaybackDevice;
        set => this.RaiseAndSetIfChanged(ref this._selectedPlaybackDevice, value);
    }

    private string? _elapsedRecordingTime = "00:00:00";

    public string? ElapsedRecordingTime
    {
        get => this._elapsedRecordingTime;
        set => this.RaiseAndSetIfChanged(ref this._elapsedRecordingTime, value);
    }

    private TimeSpan _elapsedTime;

    private TimeSpan ElapsedTime
    {
        get => this._elapsedTime;
        set => this.RaiseAndSetIfChanged(ref this._elapsedTime, value);
    }

    #endregion
}