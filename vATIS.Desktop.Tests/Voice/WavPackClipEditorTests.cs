// <copyright file="WavPackClipEditorTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;
using Vatsim.Vatis.Voice.WavPack;
using Xunit;

namespace Vatsim.Vatis.Tests.Voice;

public class WavPackClipEditorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "vatis-clipeditor-" + Guid.NewGuid().ToString("N"));

    public WavPackClipEditorTests()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllBytes(Path.Combine(_dir, "a.wav"), [0]);
        File.WriteAllBytes(Path.Combine(_dir, "b.wav"), [0]);
        File.WriteAllText(Path.Combine(_dir, "manifest.json"),
            "{\"gapMs\":42,\"clips\":{\"ONE\":\"a.wav\",\"TWO\":\"b.wav\"}}");
    }

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void Load_ListsClipsAndFilters()
    {
        var vm = new WavPackClipEditorViewModel();
        vm.Load(_dir);

        Assert.True(vm.IsLoaded);
        Assert.Equal(["ONE", "TWO"], vm.Clips.Select(c => c.Key));

        vm.Filter = "tw";
        Assert.Equal(["TWO"], vm.Clips.Select(c => c.Key));
    }

    [Fact]
    public void Save_WritesEditsAndKeepsOtherManifestFields()
    {
        var vm = new WavPackClipEditorViewModel();
        vm.Load(_dir);
        vm.Clips[0].Key = "UNO";
        Assert.True(vm.IsDirty);

        vm.SaveCommand.Execute().Subscribe();

        var manifest = WavPackManifestStore.Load(_dir);
        Assert.Equal(42, manifest.GapMs);
        Assert.Equal("a.wav", manifest.Clips["UNO"]);
        Assert.False(manifest.Clips.ContainsKey("ONE"));
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void Save_DuplicateOrMissingFile_IsRejected()
    {
        var vm = new WavPackClipEditorViewModel();
        vm.Load(_dir);

        vm.Clips[0].Key = "two";
        vm.SaveCommand.Execute().Subscribe();
        Assert.Contains("duplicate", vm.Status);

        vm.Clips[0].Key = "ONE";
        vm.Clips[0].File = "nope.wav";
        vm.SaveCommand.Execute().Subscribe();
        Assert.Contains("missing file", vm.Status);
        Assert.Equal("a.wav", WavPackManifestStore.Load(_dir).Clips["ONE"]);
    }

    [Fact]
    public void LoadClipPcm_DecodesClipAndRejectsBadPaths()
    {
        var wav = new byte[44 + 4];
        System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
        BitConverter.GetBytes(40).CopyTo(wav, 4);
        System.Text.Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(wav, 8);
        BitConverter.GetBytes(16).CopyTo(wav, 16);
        BitConverter.GetBytes((short)1).CopyTo(wav, 20);
        BitConverter.GetBytes((short)1).CopyTo(wav, 22);
        BitConverter.GetBytes(48000).CopyTo(wav, 24);
        BitConverter.GetBytes(96000).CopyTo(wav, 28);
        BitConverter.GetBytes((short)2).CopyTo(wav, 32);
        BitConverter.GetBytes((short)16).CopyTo(wav, 34);
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
        BitConverter.GetBytes(4).CopyTo(wav, 40);
        File.WriteAllBytes(Path.Combine(_dir, "a.wav"), wav);

        var vm = new WavPackClipEditorViewModel();
        vm.Load(_dir);

        Assert.Equal(4, vm.LoadClipPcm(vm.Clips[0]).Length);

        vm.Clips[0].File = "../outside.wav";
        Assert.Throws<WavPackException>(() => vm.LoadClipPcm(vm.Clips[0]));
        vm.Clips[0].File = "missing.wav";
        Assert.Throws<WavPackException>(() => vm.LoadClipPcm(vm.Clips[0]));
    }

    [Fact]
    public void Remove_DropsRowOnSave()
    {
        var vm = new WavPackClipEditorViewModel();
        vm.Load(_dir);

        vm.RemoveClipCommand.Execute(vm.Clips[0]).Subscribe();
        vm.SaveCommand.Execute().Subscribe();

        Assert.Equal(["TWO"], WavPackManifestStore.Load(_dir).Clips.Keys);
    }
}
