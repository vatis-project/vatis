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

/// <summary>
/// Provides functionality and state management for the Voice Record ATIS dialog.
/// </summary>
public class VoiceRecordAtisDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable disposables = new();
    private readonly Timer elapsedTimeUpdateTimer;
    private readonly Timer maxRecordingDurationTimer;
    private readonly Stopwatch recordingStopwatch;
    private readonly IWindowLocationService windowLocationService;
    private bool showOverlay;
    private byte[] audioBuffer = [];
    private string? atisScript;
    private bool isPlaybackEnabled;
    private bool isPlaybackActive;
    private bool isRecordingActive;
    private bool isRecordingEnabled;
    private bool deviceSelectionEnabled = true;
    private ObservableCollection<string> captureDevices = [];
    private ObservableCollection<string> playbackDevices = [];
    private string? selectedCaptureDevice;
    private string? selectedPlaybackDevice;
    private string? elapsedRecordingTime = "00:00:00";
    private TimeSpan elapsedTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceRecordAtisDialogViewModel"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="windowLocationService">The service to manage window location.</param>
    public VoiceRecordAtisDialogViewModel(
        IAppConfig appConfig,
        IWindowLocationService windowLocationService)
    {
        this.windowLocationService = windowLocationService;
        this.recordingStopwatch = new Stopwatch();
        this.elapsedTimeUpdateTimer = new Timer();
        this.elapsedTimeUpdateTimer.Interval = 50;
        this.elapsedTimeUpdateTimer.Elapsed += (_, _) =>
        {
            this.ElapsedRecordingTime = this.recordingStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            this.ElapsedTime = this.recordingStopwatch.Elapsed;
        };

        this.maxRecordingDurationTimer = new Timer();
        this.maxRecordingDurationTimer.Interval = 180000; // 3 minutes
        this.maxRecordingDurationTimer.AutoReset = false;
        this.maxRecordingDurationTimer.Elapsed += (sender, args) =>
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
                (recordingActive, buffer) => !recordingActive && buffer.Length > 0));

        this.disposables.Add(this.CancelCommand);
        this.disposables.Add(this.SaveCommand);
        this.disposables.Add(this.StartRecordingCommand);
        this.disposables.Add(this.StopRecordingCommand);
        this.disposables.Add(this.ListenCommand);

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

    /// <summary>
    /// Gets or sets a value indicating whether the overlay should be displayed in the UI.
    /// </summary>
    public bool ShowOverlay
    {
        get => this.showOverlay;
        set => this.RaiseAndSetIfChanged(ref this.showOverlay, value);
    }

    /// <summary>
    /// Gets the audio data buffer for the voice recording.
    /// </summary>
    public byte[] AudioBuffer
    {
        get => this.audioBuffer;
        private set => this.RaiseAndSetIfChanged(ref this.audioBuffer, value);
    }

    /// <summary>
    /// Gets or sets the ATIS script text.
    /// </summary>
    public string? AtisScript
    {
        get => this.atisScript;
        set => this.RaiseAndSetIfChanged(ref this.atisScript, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether playback functionality is enabled.
    /// </summary>
    public bool IsPlaybackEnabled
    {
        get => this.isPlaybackEnabled;
        set => this.RaiseAndSetIfChanged(ref this.isPlaybackEnabled, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether playback is currently active.
    /// </summary>
    public bool IsPlaybackActive
    {
        get => this.isPlaybackActive;
        set
        {
            this.RaiseAndSetIfChanged(ref this.isPlaybackActive, value);
            this.UpdateDeviceSelectionEnabled();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a recording session is currently active.
    /// </summary>
    public bool IsRecordingActive
    {
        get => this.isRecordingActive;
        set
        {
            this.RaiseAndSetIfChanged(ref this.isRecordingActive, value);
            this.UpdateDeviceSelectionEnabled();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether recording is enabled based on the selected capture and playback devices.
    /// </summary>
    public bool IsRecordingEnabled
    {
        get => this.isRecordingEnabled;
        set => this.RaiseAndSetIfChanged(ref this.isRecordingEnabled, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether device selection is enabled.
    /// </summary>
    public bool DeviceSelectionEnabled
    {
        get => this.deviceSelectionEnabled;
        set => this.RaiseAndSetIfChanged(ref this.deviceSelectionEnabled, value);
    }

    /// <summary>
    /// Gets or sets the collection of available audio capture devices.
    /// </summary>
    public ObservableCollection<string> CaptureDevices
    {
        get => this.captureDevices;
        set => this.RaiseAndSetIfChanged(ref this.captureDevices, value);
    }

    /// <summary>
    /// Gets or sets the collection of available playback devices for selection.
    /// </summary>
    public ObservableCollection<string> PlaybackDevices
    {
        get => this.playbackDevices;
        set => this.RaiseAndSetIfChanged(ref this.playbackDevices, value);
    }

    /// <summary>
    /// Gets or sets the name of the currently selected audio capture device.
    /// </summary>
    public string? SelectedCaptureDevice
    {
        get => this.selectedCaptureDevice;
        set => this.RaiseAndSetIfChanged(ref this.selectedCaptureDevice, value);
    }

    /// <summary>
    /// Gets or sets the name of the currently selected playback device.
    /// </summary>
    public string? SelectedPlaybackDevice
    {
        get => this.selectedPlaybackDevice;
        set => this.RaiseAndSetIfChanged(ref this.selectedPlaybackDevice, value);
    }

    /// <summary>
    /// Gets or sets the elapsed recording time as a formatted string in "hh:mm:ss" format.
    /// </summary>
    public string? ElapsedRecordingTime
    {
        get => this.elapsedRecordingTime;
        set => this.RaiseAndSetIfChanged(ref this.elapsedRecordingTime, value);
    }

    /// <summary>
    /// Gets or sets the total elapsed time associated with the voice recording process.
    /// </summary>
    public TimeSpan ElapsedTime
    {
        get => this.elapsedTime;
        set => this.RaiseAndSetIfChanged(ref this.elapsedTime, value);
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
    /// Releases all resources used by the <see cref="VoiceRecordAtisDialogViewModel"/> class.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if an operation is performed on an already disposed instance.
    /// </exception>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.disposables.Dispose();
    }

    /// <summary>
    /// Updates the position of the specified window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position needs to be updated. Can be null.</param>
    public void UpdatePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this.windowLocationService.Update(window);
    }

    /// <summary>
    /// Restores the position of the specified window to its previously saved state.
    /// </summary>
    /// <param name="window">The window whose position is to be restored.</param>
    public void RestorePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this.windowLocationService.Restore(window);
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
            this.elapsedTimeUpdateTimer.Start();
            this.recordingStopwatch.Start();
            this.maxRecordingDurationTimer.Start();
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

        this.recordingStopwatch.Reset();
        this.elapsedTimeUpdateTimer.Stop();
        this.maxRecordingDurationTimer.Stop();
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
}
