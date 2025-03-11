// <copyright file="VoiceRecordAtisDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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

/// <summary>
/// Provides functionality and state management for the Voice Record ATIS dialog.
/// </summary>
public class VoiceRecordAtisDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IWindowLocationService _windowLocationService;
    private readonly Stopwatch _recordingStopwatch;
    private readonly System.Timers.Timer _elapsedTimeUpdateTimer;
    private readonly System.Timers.Timer _maxRecordingDurationTimer;
    private bool _showOverlay;
    private byte[] _audioBuffer = [];
    private string? _atisScript;
    private bool _isPlaybackEnabled;
    private bool _isPlaybackActive;
    private bool _isRecordingActive;
    private bool _isRecordingEnabled;
    private bool _deviceSelectionEnabled = true;
    private ObservableCollection<string> _captureDevices = [];
    private ObservableCollection<string> _playbackDevices = [];
    private string? _selectedCaptureDevice;
    private string? _selectedPlaybackDevice;
    private string? _elapsedRecordingTime = "00:00:00";
    private TimeSpan _elapsedTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceRecordAtisDialogViewModel"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="windowLocationService">The service to manage window location.</param>
    public VoiceRecordAtisDialogViewModel(IAppConfig appConfig, IWindowLocationService windowLocationService)
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

                _ = MessageBox.ShowDialog(DialogOwner,
                    "Maximum ATIS recording duration reached (3 minutes). Recording stopped.",
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
                !string.IsNullOrEmpty(capture) && !string.IsNullOrEmpty(playback) && !playbackActive &&
                !recordingActive));
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

    /// <summary>
    /// Gets or sets a value indicating whether the overlay should be displayed in the UI.
    /// </summary>
    public bool ShowOverlay
    {
        get => _showOverlay;
        set => this.RaiseAndSetIfChanged(ref _showOverlay, value);
    }

    /// <summary>
    /// Gets the audio data buffer for the voice recording.
    /// </summary>
    public byte[] AudioBuffer
    {
        get => _audioBuffer;
        private set => this.RaiseAndSetIfChanged(ref _audioBuffer, value);
    }

    /// <summary>
    /// Gets or sets the ATIS script text.
    /// </summary>
    public string? AtisScript
    {
        get => _atisScript;
        set => this.RaiseAndSetIfChanged(ref _atisScript, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether playback functionality is enabled.
    /// </summary>
    public bool IsPlaybackEnabled
    {
        get => _isPlaybackEnabled;
        set => this.RaiseAndSetIfChanged(ref _isPlaybackEnabled, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether playback is currently active.
    /// </summary>
    public bool IsPlaybackActive
    {
        get => _isPlaybackActive;
        set
        {
            this.RaiseAndSetIfChanged(ref _isPlaybackActive, value);
            UpdateDeviceSelectionEnabled();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a recording session is currently active.
    /// </summary>
    public bool IsRecordingActive
    {
        get => _isRecordingActive;
        set
        {
            this.RaiseAndSetIfChanged(ref _isRecordingActive, value);
            UpdateDeviceSelectionEnabled();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether recording is enabled based on the selected capture and playback devices.
    /// </summary>
    public bool IsRecordingEnabled
    {
        get => _isRecordingEnabled;
        set => this.RaiseAndSetIfChanged(ref _isRecordingEnabled, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether device selection is enabled.
    /// </summary>
    public bool DeviceSelectionEnabled
    {
        get => _deviceSelectionEnabled;
        set => this.RaiseAndSetIfChanged(ref _deviceSelectionEnabled, value);
    }

    /// <summary>
    /// Gets or sets the collection of available audio capture devices.
    /// </summary>
    public ObservableCollection<string> CaptureDevices
    {
        get => _captureDevices;
        set => this.RaiseAndSetIfChanged(ref _captureDevices, value);
    }

    /// <summary>
    /// Gets or sets the collection of available playback devices for selection.
    /// </summary>
    public ObservableCollection<string> PlaybackDevices
    {
        get => _playbackDevices;
        set => this.RaiseAndSetIfChanged(ref _playbackDevices, value);
    }

    /// <summary>
    /// Gets or sets the name of the currently selected audio capture device.
    /// </summary>
    public string? SelectedCaptureDevice
    {
        get => _selectedCaptureDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedCaptureDevice, value);
    }

    /// <summary>
    /// Gets or sets the name of the currently selected playback device.
    /// </summary>
    public string? SelectedPlaybackDevice
    {
        get => _selectedPlaybackDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedPlaybackDevice, value);
    }

    /// <summary>
    /// Gets or sets the elapsed recording time as a formatted string in "hh:mm:ss" format.
    /// </summary>
    public string? ElapsedRecordingTime
    {
        get => _elapsedRecordingTime;
        set => this.RaiseAndSetIfChanged(ref _elapsedRecordingTime, value);
    }

    /// <summary>
    /// Gets or sets the total elapsed time associated with the voice recording process.
    /// </summary>
    public TimeSpan ElapsedTime
    {
        get => _elapsedTime;
        set => this.RaiseAndSetIfChanged(ref _elapsedTime, value);
    }

    /// <summary>
    /// Gets the command that is executed to cancel the current operation and close the dialog.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CancelCommand { get; }

    /// <summary>
    /// Gets the command that is executed to save the recording and close the current dialog.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> SaveCommand { get; }

    /// <summary>
    /// Gets the command that is executed to initiate the recording process.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartRecordingCommand { get; }

    /// <summary>
    /// Gets the command that is executed to stop the recording process.
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopRecordingCommand { get; }

    /// <summary>
    /// Gets the command that is executed to play back the recorded audio for listening.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ListenCommand { get; }

    /// <summary>
    /// Gets or sets the owner of the dialog window.
    /// </summary>
    public Window? DialogOwner { get; set; }

    /// <summary>
    /// Updates the position of the specified window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position needs to be updated. Ignored if null.</param>
    public void UpdatePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Update(window);
    }

    /// <summary>
    /// Restores the position of the specified window to its previously saved state.
    /// </summary>
    /// <param name="window">The window whose position is to be restored. Ignored if null.</param>
    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Restore(window);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();

        _elapsedTimeUpdateTimer.Stop();
        _elapsedTimeUpdateTimer.Dispose();

        _maxRecordingDurationTimer.Stop();
        _maxRecordingDurationTimer.Dispose();

        GC.SuppressFinalize(this);
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
}
