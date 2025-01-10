using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services;
using Vatsim.Vatis.Voice.Audio;

namespace Vatsim.Vatis.Ui.ViewModels;

public class VoiceRecordAtisDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IWindowLocationService _windowLocationService;
    private readonly Stopwatch _recordingStopwatch;
    private readonly System.Timers.Timer _elapsedTimeUpdateTimer;
    private readonly System.Timers.Timer _maxRecordingDurationTimer;

    #region Reactive Properties
    private bool _showOverlay;
    public bool ShowOverlay
    {
        get => _showOverlay;
        set => this.RaiseAndSetIfChanged(ref _showOverlay, value);
    }

    private byte[] _audioBuffer = [];
    public byte[] AudioBuffer
    {
        get => _audioBuffer;
        private set => this.RaiseAndSetIfChanged(ref _audioBuffer, value);
    }

    private string? _atisScript;
    public string? AtisScript
    {
        get => _atisScript;
        set => this.RaiseAndSetIfChanged(ref _atisScript, value);
    }

    private bool _isPlaybackEnabled;
    public bool IsPlaybackEnabled
    {
        get => _isPlaybackEnabled;
        set => this.RaiseAndSetIfChanged(ref _isPlaybackEnabled, value);
    }

    private bool _isPlaybackActive;
    public bool IsPlaybackActive
    {
        get => _isPlaybackActive;
        set
        {
            this.RaiseAndSetIfChanged(ref _isPlaybackActive, value);
            UpdateDeviceSelectionEnabled();
        }
    }

    private bool _isRecordingActive;
    private bool IsRecordingActive
    {
        get => _isRecordingActive;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRecordingActive, value);
            UpdateDeviceSelectionEnabled();
        }
    }

    private bool _isRecordingEnabled;
    public bool IsRecordingEnabled
    {
        get => _isRecordingEnabled;
        set => this.RaiseAndSetIfChanged(ref _isRecordingEnabled, value);
    }

    private bool _deviceSelectionEnabled = true;
    public bool DeviceSelectionEnabled
    {
        get => _deviceSelectionEnabled;
        set => this.RaiseAndSetIfChanged(ref _deviceSelectionEnabled, value);
    }

    private ObservableCollection<string> _captureDevices = [];
    public ObservableCollection<string> CaptureDevices
    {
        get => _captureDevices;
        set => this.RaiseAndSetIfChanged(ref _captureDevices, value);
    }

    private ObservableCollection<string> _playbackDevices = [];
    public ObservableCollection<string> PlaybackDevices
    {
        get => _playbackDevices;
        set => this.RaiseAndSetIfChanged(ref _playbackDevices, value);
    }

    private string? _selectedCaptureDevice;
    public string? SelectedCaptureDevice
    {
        get => _selectedCaptureDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedCaptureDevice, value);
    }

    private string? _selectedPlaybackDevice;
    public string? SelectedPlaybackDevice
    {
        get => _selectedPlaybackDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedPlaybackDevice, value);
    }

    private string? _elapsedRecordingTime = "00:00:00";
    public string? ElapsedRecordingTime
    {
        get => _elapsedRecordingTime;
        set => this.RaiseAndSetIfChanged(ref _elapsedRecordingTime, value);
    }

    private TimeSpan _elapsedTime;
    private TimeSpan ElapsedTime
    {
        get => _elapsedTime;
        set => this.RaiseAndSetIfChanged(ref _elapsedTime, value);
    }
    #endregion

    public ReactiveCommand<ICloseable, Unit> CancelCommand { get; }
    public ReactiveCommand<ICloseable, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> StartRecordingCommand { get; }
    public ReactiveCommand<Unit, Unit> StopRecordingCommand { get; }
    public ReactiveCommand<Unit, Unit> ListenCommand { get; }
    public Window? DialogOwner { get; set; }

    public VoiceRecordAtisDialogViewModel(
        IAppConfig appConfig,
        IWindowLocationService windowLocationService)
    {
        _windowLocationService = windowLocationService;
        _recordingStopwatch = new Stopwatch();
        _elapsedTimeUpdateTimer = new System.Timers.Timer();
        _elapsedTimeUpdateTimer.Interval = 50;
        _elapsedTimeUpdateTimer.Elapsed += (_, _) =>
        {
            ElapsedRecordingTime = _recordingStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            ElapsedTime = _recordingStopwatch.Elapsed;
        };

        _maxRecordingDurationTimer = new System.Timers.Timer();
        _maxRecordingDurationTimer.Interval = 180000; // 3 minutes
        _maxRecordingDurationTimer.AutoReset = false;
        _maxRecordingDurationTimer.Elapsed += (sender, args) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                HandleStopRecordingCommand();

                ArgumentNullException.ThrowIfNull(DialogOwner);

                _ = MessageBox.ShowDialog(DialogOwner, "Maximum ATIS recording duration reached (3 minutes). Recording stopped.",
                    "Warning", MessageBoxButton.Ok, MessageBoxIcon.Information);
            });
        };

        SaveCommand = ReactiveCommand.Create<ICloseable>(HandleSaveCommand,
            this.WhenAnyValue(
                x => x.AudioBuffer,
                x => x.ElapsedTime,
                (buffer, elapsed) => buffer.Length > 0 && elapsed >= TimeSpan.FromSeconds(5)));
        CancelCommand = ReactiveCommand.Create<ICloseable>(HandleCancelCommand);
        StartRecordingCommand = ReactiveCommand.Create(HandleStartRecordingCommand, this.WhenAnyValue(
                x => x.SelectedCaptureDevice,
                x => x.SelectedPlaybackDevice,
                x => x.IsPlaybackActive,
                x => x.IsRecordingActive,
                (capture, playback, playbackActive, recordingActive) =>
                    !string.IsNullOrEmpty(capture) && !string.IsNullOrEmpty(playback) && !playbackActive && !recordingActive));
        StopRecordingCommand = ReactiveCommand.Create(HandleStopRecordingCommand, this.WhenAnyValue(
            x => x.IsPlaybackActive,
            x => x.IsRecordingActive,
            (playbackActive, recordingActive) => !playbackActive && recordingActive));
        ListenCommand = ReactiveCommand.Create(HandleListenCommand, this.WhenAnyValue(
            x => x.IsRecordingActive,
            x => x.AudioBuffer,
            (recordingActive, audioBuffer) => !recordingActive && audioBuffer.Length > 0));

        _disposables.Add(CancelCommand);
        _disposables.Add(SaveCommand);
        _disposables.Add(StartRecordingCommand);
        _disposables.Add(StopRecordingCommand);
        _disposables.Add(ListenCommand);

        NativeAudio.GetCaptureDevices((idPtr, namePtr, _) =>
        {
            var id = Marshal.PtrToStringAnsi(idPtr);
            if (id != null)
            {
                var name = Marshal.PtrToStringAnsi(namePtr);
                if (name != null)
                {
                    if (name == appConfig.MicrophoneDevice)
                    {
                        SelectedCaptureDevice = name;
                    }

                    CaptureDevices.Add(name);
                }
            }
        });

        NativeAudio.GetPlaybackDevices((idPtr, namePtr, _) =>
        {
            var id = Marshal.PtrToStringAnsi(idPtr);
            if (id != null)
            {
                var name = Marshal.PtrToStringAnsi(namePtr);
                if (name != null)
                {
                    if (name == appConfig.PlaybackDevice)
                    {
                        SelectedPlaybackDevice = name;
                    }

                    PlaybackDevices.Add(name);
                }
            }
        });

        this.WhenAnyValue(x => x.SelectedPlaybackDevice).Subscribe(_ =>
        {
            appConfig.PlaybackDevice = SelectedPlaybackDevice;
            appConfig.SaveConfig();
            NativeAudio.DestroyDevices();
        });

        this.WhenAnyValue(x => x.SelectedCaptureDevice).Subscribe(_ =>
        {
            appConfig.MicrophoneDevice = SelectedCaptureDevice;
            appConfig.SaveConfig();
            NativeAudio.DestroyDevices();
        });

        this.WhenAnyValue(
            x => x.SelectedCaptureDevice,
            x => x.SelectedPlaybackDevice,
            (capture, playback) =>
                !string.IsNullOrWhiteSpace(capture) &&
                !string.IsNullOrWhiteSpace(playback)).Subscribe(x => IsRecordingEnabled = x);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                ShowOverlay = lifetime.Windows.Count(w => w.GetType() != typeof(Windows.MainWindow)) > 1;
            };
        }
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
        if (SelectedCaptureDevice == null)
            return;

        if (NativeAudio.StartRecording(SelectedCaptureDevice))
        {
            AudioBuffer = [];
            IsRecordingEnabled = false;
            IsRecordingActive = true;
            IsPlaybackEnabled = false;

            ElapsedRecordingTime = "00:00:00";
            _elapsedTimeUpdateTimer.Start();
            _recordingStopwatch.Start();
            _maxRecordingDurationTimer.Start();
        }
    }

    private void HandleStopRecordingCommand()
    {
        var temp = new List<byte[]>();
        NativeAudio.StopRecording((data, dataSize) =>
        {
            if (data == IntPtr.Zero)
                return;

            var buffer = new byte[dataSize];
            Marshal.Copy(data, buffer, 0, dataSize);
            temp.Add(buffer);
        });
        AudioBuffer = CombineAudioBuffers(temp);
        IsRecordingEnabled = true;
        IsRecordingActive = false;
        IsPlaybackEnabled = true;

        _recordingStopwatch.Reset();
        _elapsedTimeUpdateTimer.Stop();
        _maxRecordingDurationTimer.Stop();
    }

    private void HandleListenCommand()
    {
        if (SelectedPlaybackDevice == null)
            return;

        if (IsPlaybackActive)
        {
            if (NativeAudio.StopPlayback())
            {
                IsRecordingEnabled = true;
                IsPlaybackActive = false;
            }

            return;
        }

        if (NativeAudio.StartPlayback(SelectedPlaybackDevice))
        {
            IsPlaybackActive = true;
            IsRecordingEnabled = false;
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
        DeviceSelectionEnabled = !IsPlaybackActive && !IsRecordingActive;
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
            return;

        _windowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Restore(window);
    }

    public void Dispose()
    {
       GC.SuppressFinalize(this);
       _disposables.Dispose();
    }
}
