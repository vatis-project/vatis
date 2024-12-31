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
    private readonly CompositeDisposable mDisposables = new();
    private readonly IWindowLocationService mWindowLocationService;
    private readonly Stopwatch mRecordingStopwatch;
    private readonly System.Timers.Timer mElapsedTimeUpdateTimer;
    private readonly System.Timers.Timer mMaxRecordingDurationTimer;
    
    #region Reactive Properties

    private bool mShowOverlay;
    public bool ShowOverlay
    {
        get => mShowOverlay;
        set => this.RaiseAndSetIfChanged(ref mShowOverlay, value);
    }
    
    private byte[] mAudioBuffer = [];
    public byte[] AudioBuffer
    {
        get => mAudioBuffer;
        private set => this.RaiseAndSetIfChanged(ref mAudioBuffer, value);
    }

    private string? mAtisScript;
    public string? AtisScript
    {
        get => mAtisScript;
        set => this.RaiseAndSetIfChanged(ref mAtisScript, value);
    }

    private bool mIsPlaybackEnabled;
    public bool IsPlaybackEnabled
    {
        get => mIsPlaybackEnabled;
        set => this.RaiseAndSetIfChanged(ref mIsPlaybackEnabled, value);
    }

    private bool mIsPlaybackActive;
    public bool IsPlaybackActive
    {
        get => mIsPlaybackActive;
        set
        {
            this.RaiseAndSetIfChanged(ref mIsPlaybackActive, value);
            UpdateDeviceSelectionEnabled();
        }
    }

    private bool mIsRecordingActive;
    private bool IsRecordingActive
    {
        get => mIsRecordingActive;
        set
        {
            this.RaiseAndSetIfChanged(ref mIsRecordingActive, value);
            UpdateDeviceSelectionEnabled();
        }
    }

    private bool mIsRecordingEnabled;
    public bool IsRecordingEnabled
    {
        get => mIsRecordingEnabled;
        set => this.RaiseAndSetIfChanged(ref mIsRecordingEnabled, value);
    }

    private bool mDeviceSelectionEnabled = true;
    public bool DeviceSelectionEnabled
    {
        get => mDeviceSelectionEnabled;
        set => this.RaiseAndSetIfChanged(ref mDeviceSelectionEnabled, value);
    }

    private ObservableCollection<string> mCaptureDevices = [];
    public ObservableCollection<string> CaptureDevices
    {
        get => mCaptureDevices;
        set => this.RaiseAndSetIfChanged(ref mCaptureDevices, value);
    }

    private ObservableCollection<string> mPlaybackDevices = [];
    public ObservableCollection<string> PlaybackDevices
    {
        get => mPlaybackDevices;
        set => this.RaiseAndSetIfChanged(ref mPlaybackDevices, value);
    }

    private string? mSelectedCaptureDevice;
    public string? SelectedCaptureDevice
    {
        get => mSelectedCaptureDevice;
        set => this.RaiseAndSetIfChanged(ref mSelectedCaptureDevice, value);
    }
    
    private string? mSelectedPlaybackDevice;
    public string? SelectedPlaybackDevice
    {
        get => mSelectedPlaybackDevice;
        set => this.RaiseAndSetIfChanged(ref mSelectedPlaybackDevice, value);
    }

    private string? mElapsedRecordingTime = "00:00:00";
    public string? ElapsedRecordingTime
    {
        get => mElapsedRecordingTime;
        set => this.RaiseAndSetIfChanged(ref mElapsedRecordingTime, value);
    }

    private TimeSpan mElapsedTime;
    private TimeSpan ElapsedTime
    {
        get => mElapsedTime;
        set => this.RaiseAndSetIfChanged(ref mElapsedTime, value);
    }

    #endregion

    public ReactiveCommand<ICloseable, Unit> CancelCommand { get; private set; }
    public ReactiveCommand<ICloseable, Unit> SaveCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> StartRecordingCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> StopRecordingCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> ListenCommand { get; private set; }
    public Window? DialogOwner { get; set; }

    public VoiceRecordAtisDialogViewModel(
        IAppConfig appConfig,
        IWindowLocationService windowLocationService)
    {
        mWindowLocationService = windowLocationService;
        mRecordingStopwatch = new Stopwatch();
        mElapsedTimeUpdateTimer = new System.Timers.Timer();
        mElapsedTimeUpdateTimer.Interval = 50;
        mElapsedTimeUpdateTimer.Elapsed += (_, _) =>
        {
            ElapsedRecordingTime = mRecordingStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            ElapsedTime = mRecordingStopwatch.Elapsed;
        };

        mMaxRecordingDurationTimer = new System.Timers.Timer();
        mMaxRecordingDurationTimer.Interval = 180000; // 3 minutes
        mMaxRecordingDurationTimer.AutoReset = false;
        mMaxRecordingDurationTimer.Elapsed += (sender, args) =>
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
        
        mDisposables.Add(CancelCommand);
        mDisposables.Add(SaveCommand);
        mDisposables.Add(StartRecordingCommand);
        mDisposables.Add(StopRecordingCommand);
        mDisposables.Add(ListenCommand);

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
            mElapsedTimeUpdateTimer.Start();
            mRecordingStopwatch.Start();
            mMaxRecordingDurationTimer.Start();
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
        
        mRecordingStopwatch.Reset();
        mElapsedTimeUpdateTimer.Stop();
        mMaxRecordingDurationTimer.Stop();
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

        mWindowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        mWindowLocationService.Restore(window);
    }

    public void Dispose()
    {
       GC.SuppressFinalize(this);
       mDisposables.Dispose();
    }
}