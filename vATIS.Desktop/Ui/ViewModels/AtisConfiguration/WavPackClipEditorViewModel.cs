// <copyright file="WavPackClipEditorViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using Avalonia.Platform.Storage;
using ReactiveUI;
using Vatsim.Vatis.Utils;
using Vatsim.Vatis.Voice.Audio;
using Vatsim.Vatis.Voice.WavPack;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Edits the clip mappings (spoken phrase to WAV file) of a voice pack's manifest.json.
/// </summary>
public class WavPackClipEditorViewModel : ReactiveObject
{
    private readonly List<WavPackClipEntry> _all = [];
    private WavPackManifest? _manifest;
    private string? _packFolder;
    private string _filter = string.Empty;
    private bool _isDirty;
    private string? _status;
    private bool _isPlaying;
    private System.Threading.CancellationTokenSource? _playbackCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="WavPackClipEditorViewModel"/> class.
    /// </summary>
    public WavPackClipEditorViewModel()
    {
        AddClipsCommand = ReactiveCommand.CreateFromTask(HandleAddClips);
        RemoveClipCommand = ReactiveCommand.Create<WavPackClipEntry?>(HandleRemoveClip);
        SaveCommand = ReactiveCommand.Create(HandleSave);
        PlayClipCommand = ReactiveCommand.Create<WavPackClipEntry?>(HandlePlayClip);
    }

    /// <summary>
    /// Gets the command that adds clips by choosing WAV files from the pack folder.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddClipsCommand { get; }

    /// <summary>
    /// Gets the command that removes the given row.
    /// </summary>
    public ReactiveCommand<WavPackClipEntry?, Unit> RemoveClipCommand { get; }

    /// <summary>
    /// Gets the command that writes the clips back to manifest.json.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>
    /// Gets the command that plays the given row's WAV file.
    /// </summary>
    public ReactiveCommand<WavPackClipEntry?, Unit> PlayClipCommand { get; }

    /// <summary>
    /// Gets the rows matching the current filter.
    /// </summary>
    public ObservableCollection<WavPackClipEntry> Clips { get; } = [];

    /// <summary>
    /// Gets or sets the text used to filter rows by phrase or file name.
    /// </summary>
    public string Filter
    {
        get => _filter;
        set
        {
            this.RaiseAndSetIfChanged(ref _filter, value);
            ApplyFilter();
        }
    }

    /// <summary>
    /// Gets a value indicating whether there are edits not yet saved to manifest.json.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    /// <summary>
    /// Gets the result of the last save or load.
    /// </summary>
    public string? Status
    {
        get => _status;
        private set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    /// <summary>
    /// Gets a value indicating whether a clip is currently playing.
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isPlaying, value);
            this.RaisePropertyChanged(nameof(PlayButtonText));
        }
    }

    /// <summary>
    /// Gets the label of the play button: "Stop" while a clip plays, otherwise "Play".
    /// </summary>
    public string PlayButtonText => _isPlaying ? "Stop" : "Play";

    /// <summary>
    /// Gets a value indicating whether a manifest is loaded.
    /// </summary>
    public bool IsLoaded => _manifest != null;

    /// <summary>
    /// Loads the manifest in the folder. Clears the editor if there is none.
    /// </summary>
    /// <param name="packFolder">The pack folder, or null.</param>
    public void Load(string? packFolder)
    {
        StopPlayback();
        foreach (var e in _all)
        {
            e.PropertyChanged -= OnEntryChanged;
        }

        _all.Clear();
        _manifest = null;
        _packFolder = packFolder;
        IsDirty = false;

        if (!string.IsNullOrWhiteSpace(packFolder))
        {
            try
            {
                _manifest = WavPackManifestStore.Load(packFolder);
                foreach (var (key, file) in _manifest.Clips.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    var entry = new WavPackClipEntry { Key = key, File = file };
                    entry.PropertyChanged += OnEntryChanged;
                    _all.Add(entry);
                }

                Status = null;
            }
            catch (WavPackException ex)
            {
                Status = ex.Message;
            }
        }
        else
        {
            Status = null;
        }

        ApplyFilter();
        this.RaisePropertyChanged(nameof(IsLoaded));
    }

    /// <summary>
    /// Decodes a row's WAV file to the 48 kHz mono 16-bit PCM bytes used for playback.
    /// </summary>
    /// <param name="entry">The row to decode.</param>
    /// <returns>The PCM bytes.</returns>
    /// <exception cref="WavPackException">The file is missing, outside the pack folder, or not a supported WAV.</exception>
    public byte[] LoadClipPcm(WavPackClipEntry entry)
    {
        if (_packFolder == null)
        {
            throw new WavPackException("No voice pack is loaded.");
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_packFolder));
        var full = Path.GetFullPath(Path.Combine(root, entry.File.Trim()));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(full))
        {
            throw new WavPackException($"File not found: {entry.File}");
        }

        try
        {
            var samples = WavDecoder.Decode(File.ReadAllBytes(full));
            var pcm = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, pcm, 0, pcm.Length);
            return pcm;
        }
        catch (InvalidOperationException ex)
        {
            throw new WavPackException($"{entry.File}: {ex.Message}");
        }
    }

    private void HandlePlayClip(WavPackClipEntry? entry)
    {
        if (IsPlaying)
        {
            StopPlayback();
            return;
        }

        if (entry == null)
        {
            return;
        }

        try
        {
            var pcm = LoadClipPcm(entry);
            if (pcm.Length == 0)
            {
                Status = $"{entry.File} contains no audio.";
                return;
            }

            if (!NativeAudio.StartBufferPlayback(pcm, pcm.Length))
            {
                Status = "Unable to start playback. Check your audio output device.";
                return;
            }

            IsPlaying = true;

            // The native player repeats the buffer until it is stopped, so stop it once the clip has finished.
            var cts = new System.Threading.CancellationTokenSource();
            _playbackCts = cts;
            var durationMs = (int)((long)pcm.Length / 2 * 1000 / WavDecoder.TargetSampleRate) + 150;
            _ = System.Threading.Tasks.Task.Delay(durationMs, cts.Token).ContinueWith(
                t =>
                {
                    if (!t.IsCanceled)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            if (_playbackCts == cts)
                            {
                                StopPlayback();
                            }
                        });
                    }
                },
                System.Threading.Tasks.TaskScheduler.Default);
        }
        catch (Exception ex) when (ex is WavPackException or IOException)
        {
            Status = ex.Message;
        }
    }

    private void StopPlayback()
    {
        if (!_isPlaying)
        {
            return;
        }

        _playbackCts?.Cancel();
        _playbackCts = null;
        NativeAudio.StopBufferPlayback();
        IsPlaying = false;
    }

    private void OnEntryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => IsDirty = true;

    private void ApplyFilter()
    {
        Clips.Clear();
        foreach (var e in _all.Where(e => _filter.Length == 0 ||
                                          e.Key.Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
                                          e.File.Contains(_filter, StringComparison.OrdinalIgnoreCase)))
        {
            Clips.Add(e);
        }
    }

    private async System.Threading.Tasks.Task HandleAddClips()
    {
        if (_manifest == null || _packFolder == null)
        {
            Status = "Select a pack folder with a manifest.json first.";
            return;
        }

        var filters = new List<FilePickerFileType> { new("WAV audio") { Patterns = ["*.wav"] } };
        var files = await FilePickerExtensions.OpenFilePickerAsync(filters, "Add WAV clips");
        if (files == null)
        {
            return;
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_packFolder));
        var added = 0;
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                Status = $"{Path.GetFileName(file)} is outside the pack folder; copy it there first.";
                continue;
            }

            var entry = new WavPackClipEntry
            {
                Key = Path.GetFileNameWithoutExtension(file).Replace('_', ' ').ToUpperInvariant(),
                File = relative.Replace('\\', '/')
            };
            entry.PropertyChanged += OnEntryChanged;
            _all.Add(entry);
            added++;
        }

        if (added > 0)
        {
            IsDirty = true;
            Status = $"Added {added} clip(s). Edit the phrase for each, then save.";
            ApplyFilter();
        }
    }

    private void HandleRemoveClip(WavPackClipEntry? entry)
    {
        if (entry == null)
        {
            return;
        }

        entry.PropertyChanged -= OnEntryChanged;
        _all.Remove(entry);
        IsDirty = true;
        ApplyFilter();
    }

    private void HandleSave()
    {
        if (_manifest == null || _packFolder == null)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var clips = new Dictionary<string, string>();
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_packFolder));

        foreach (var e in _all)
        {
            var key = e.Key.Trim();
            var file = e.File.Trim();
            if (key.Length == 0)
            {
                Status = $"Not saved: {(file.Length == 0 ? "a row" : file)} has no phrase.";
                return;
            }

            if (!seen.Add(key))
            {
                Status = $"Not saved: duplicate phrase \"{key}\".";
                return;
            }

            var full = Path.GetFullPath(Path.Combine(root, file));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(full))
            {
                Status = $"Not saved: \"{key}\" points to a missing file ({file}).";
                return;
            }

            clips[key] = file;
        }

        _manifest.Clips = clips;
        try
        {
            WavPackManifestStore.Save(_packFolder, _manifest);
            WavPackLibrary.StampContentHash(_packFolder);
            IsDirty = false;
            Status = $"Saved {clips.Count} clips to manifest.json.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Status = $"Save failed: {ex.Message}";
        }
    }
}
