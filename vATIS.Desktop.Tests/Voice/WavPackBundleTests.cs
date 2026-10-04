// <copyright file="WavPackBundleTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Voice.WavPack;
using Xunit;

namespace Vatsim.Vatis.Tests.Voice;

[Collection("AppData")]
public sealed class WavPackBundleTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vatis-bundle-" + Guid.NewGuid().ToString("N"));
    private readonly string _pack;

    public WavPackBundleTests()
    {
        PathProvider.SetAppDataPath(Path.Combine(_root, "appdata"));
        _pack = Path.Combine(_root, "My Pack");
        Directory.CreateDirectory(_pack);
        File.WriteAllBytes(Path.Combine(_pack, "one.wav"), Wav());
        File.WriteAllBytes(Path.Combine(_pack, "unused.wav"), Wav());
        File.WriteAllText(Path.Combine(_pack, "ATIS.txt"), "RECORD:1:one.wav");
        File.WriteAllText(Path.Combine(_pack, "manifest.json"), "{\"gapMs\":0,\"clips\":{\"ONE\":\"one.wav\"}}");
    }

    public void Dispose()
    {
        PathProvider.SetAppDataPath(string.Empty);
        Directory.Delete(_root, true);
    }

    [Fact]
    public async Task ProfileRoundTrip_BundlesOnlyReferencedClipsAndSharesPack()
    {
        var repo = new ProfileRepository(null!);
        var pack = WavPackLibrary.Import(_pack).Pack;
        var profile = new Profile { Name = "UK" };
        profile.Stations!.Add(Station("EGLL", pack.Id));
        profile.Stations.Add(Station("EGKK", pack.Id));
        profile.Stations.Add(Station("EGCC", null));
        var zip = Path.Combine(_root, "p.zip");

        repo.Export(profile, zip);

        using (var archive = ZipFile.OpenRead(zip))
        {
            var names = archive.Entries.Select(e => e.FullName).OrderBy(x => x).ToList();
            Assert.Equal(["profile.json", "voicepacks/My Pack/manifest.json", "voicepacks/My Pack/one.wav"], names);
        }

        Assert.Equal(pack.Id, profile.Stations[0].AtisVoice.WavPackId); // original untouched

        // importing on a machine that already has the identical pack reuses it
        var imported = await repo.Import(zip);
        Assert.Equal(pack.Id, imported.Stations![0].AtisVoice.WavPackId);
        Assert.Equal(pack.Id, imported.Stations[1].AtisVoice.WavPackId);
        Assert.Null(imported.Stations[2].AtisVoice.WavPackId);
        Assert.Single(WavPackLibrary.List());

        // and on a machine without it, the pack is added to the library
        WavPackLibrary.Delete(pack.Id!);
        var imported2 = await repo.Import(zip);
        var newId = imported2.Stations![0].AtisVoice.WavPackId!;
        Assert.Equal(newId, imported2.Stations[1].AtisVoice.WavPackId);
        var folder = WavPackLibrary.FolderForId(newId);
        Assert.NotNull(folder);
        Assert.NotNull(new WavPackService().Build("ONE", folder).Pcm);
        Assert.False(File.Exists(Path.Combine(folder, "unused.wav")));
    }

    [Fact]
    public void Export_MissingPack_Throws()
    {
        var repo = new ProfileRepository(null!);
        var profile = new Profile { Name = "UK" };
        profile.Stations!.Add(Station("EGLL", "does-not-exist"));

        Assert.Throws<WavPackException>(() => repo.Export(profile, Path.Combine(_root, "p.zip")));
    }

    [Fact]
    public void ReadBundle_RejectsPathTraversal()
    {
        var zip = Path.Combine(_root, "evil.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            archive.CreateEntry("station.json");
            archive.CreateEntry("voicepacks/x/manifest.json");
            archive.CreateEntry("voicepacks/x/../../../evil.wav");
        }

        Assert.Throws<WavPackException>(() => WavPackBundler.ReadBundle(zip, "station.json"));
        Assert.False(File.Exists(Path.Combine(_root, "evil.wav")));
    }

    [Fact]
    public void Library_ImportsManifestPackCopyingOnlyReferencedClips()
    {
        var (pack, report, reused) = WavPackLibrary.Import(_pack);

        Assert.Null(report);
        Assert.False(reused);
        Assert.StartsWith(PathProvider.VoicePacksFolderPath, pack.Folder);
        Assert.True(File.Exists(Path.Combine(pack.Folder, "one.wav")));
        Assert.False(File.Exists(Path.Combine(pack.Folder, "unused.wav")));
        Assert.Equal("My Pack", pack.Name);
        Assert.NotNull(new WavPackService().Build("ONE", pack.Folder).Pcm);
    }

    [Fact]
    public void Library_ReusesIdenticalPackButNotEditedOne()
    {
        var (first, _, _) = WavPackLibrary.Import(_pack);
        var (second, _, reused) = WavPackLibrary.Import(_pack);

        Assert.True(reused);
        Assert.Equal(first.Id, second.Id);
        Assert.Single(WavPackLibrary.List());

        // editing the stored pack must not make a later import of the original resolve to the edited copy
        var manifest = WavPackManifestStore.Load(first.Folder);
        manifest.GapMs = 123;
        WavPackManifestStore.Save(first.Folder, manifest);
        WavPackLibrary.StampContentHash(first.Folder);

        var (third, _, reusedAgain) = WavPackLibrary.Import(_pack);
        Assert.False(reusedAgain);
        Assert.NotEqual(first.Id, third.Id);
        Assert.Equal("My Pack (2)", third.Name);
    }

    [Fact]
    public void Library_RenameAndDelete()
    {
        var (pack, _, _) = WavPackLibrary.Import(_pack);

        WavPackLibrary.Rename(pack.Id!, "UK ATIS");
        Assert.Equal("UK ATIS", Assert.Single(WavPackLibrary.List()).Name);
        Assert.Throws<WavPackException>(() => WavPackLibrary.Rename(pack.Id!, "  "));

        WavPackLibrary.Delete(pack.Id!);
        Assert.Empty(WavPackLibrary.List());
        Assert.Null(WavPackLibrary.FolderForId(pack.Id!));
    }

    [Fact]
    public void Library_ResolveFolderPrefersIdAndFallsBackToLegacyPath()
    {
        var (pack, _, _) = WavPackLibrary.Import(_pack);

        Assert.Equal(pack.Folder, WavPackLibrary.ResolveFolder(new AtisVoiceMeta { WavPackId = pack.Id }));
        Assert.Equal(_pack, WavPackLibrary.ResolveFolder(new AtisVoiceMeta { WavPackPath = _pack }));
        Assert.Null(WavPackLibrary.ResolveFolder(new AtisVoiceMeta { WavPackId = "../x" }));
        Assert.Null(WavPackLibrary.ResolveFolder(new AtisVoiceMeta()));
    }

    [Fact]
    public void Library_ConvertsLegacyPackWithoutTouchingSource()
    {
        File.Delete(Path.Combine(_pack, "manifest.json"));

        var (pack, report, _) = WavPackLibrary.Import(_pack);

        Assert.NotNull(report);
        Assert.False(File.Exists(Path.Combine(_pack, "manifest.json")));
        Assert.NotNull(new WavPackService().Build("ONE", pack.Folder).Pcm);
    }

    [Fact]
    public void Library_RejectsFoldersWithoutAPackOrWithMissingClips()
    {
        var empty = Path.Combine(_root, "empty");
        Directory.CreateDirectory(empty);
        Assert.Throws<WavPackException>(() => WavPackLibrary.Import(empty));

        File.Delete(Path.Combine(_pack, "one.wav"));
        Assert.Throws<WavPackException>(() => WavPackLibrary.Import(_pack));
        Assert.Empty(WavPackLibrary.List());
    }

    private static AtisStation Station(string icao, string? packId)
    {
        var station = new AtisStation { Identifier = icao, Name = icao, AtisType = AtisType.Combined };
        station.AtisVoice.UseTextToSpeech = packId == null;
        station.AtisVoice.UseWavPack = packId != null;
        station.AtisVoice.WavPackId = packId;
        return station;
    }

    private static byte[] Wav()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("RIFF"u8.ToArray());
        w.Write(36 + 1920);
        w.Write("WAVEfmt "u8.ToArray());
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(48000);
        w.Write(96000);
        w.Write((short)2);
        w.Write((short)16);
        w.Write("data"u8.ToArray());
        w.Write(1920);
        w.Write(new byte[1920]);
        return ms.ToArray();
    }
}
