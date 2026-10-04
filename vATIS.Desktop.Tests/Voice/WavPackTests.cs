// <copyright file="WavPackTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Text;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Voice.WavPack;
using Xunit;

namespace Vatsim.Vatis.Tests.Voice;

public class WavPackTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "vatis-wavpack-" + Guid.NewGuid().ToString("N"));

    public WavPackTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void Decode_16BitMono48k_ReturnsSamples()
    {
        var samples = WavDecoder.Decode(MakeWav(48000, 1, 16, [1000, -1000, 2000]));
        Assert.Equal([1000, -1000, 2000], samples);
    }

    [Fact]
    public void Decode_Stereo24k_DownmixesAndResamples()
    {
        // 100 stereo frames at 24 kHz -> 200 mono samples at 48 kHz
        var data = new short[200];
        var samples = WavDecoder.Decode(MakeWav(24000, 2, 16, data));
        Assert.Equal(200, samples.Length);
    }

    [Fact]
    public void Decode_NotWav_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => WavDecoder.Decode(new byte[32]));
    }

    [Fact]
    public void Build_StitchesTokensCaseInsensitivelyAndFrameAligned()
    {
        WritePack(("ONE", new short[960]), ("TWO", new short[960]));
        var result = new WavPackService().Build("one, two", _dir);

        Assert.NotNull(result.Pcm);
        Assert.Empty(result.MissingTokens);
        Assert.Equal(0, result.Pcm!.Length / 2 % 960);
        Assert.True(result.Pcm.Length >= 2 * 960 * 2);
    }

    [Fact]
    public void Build_MissingClip_ReportsTokens()
    {
        WritePack(("ONE", new short[960]));
        var result = new WavPackService().Build("ONE THREE FOUR", _dir);

        Assert.Null(result.Pcm);
        Assert.Equal(["THREE", "FOUR"], result.MissingTokens);
    }

    [Fact]
    public void Build_PrefersLongestPhrase()
    {
        WritePack(("GROUND", new short[960]), ("OPEN", new short[960]));
        var phrase = new short[960 * 5];
        File.WriteAllBytes(Path.Combine(_dir, "go.wav"), MakeWav(48000, 1, 16, phrase));
        File.WriteAllText(Path.Combine(_dir, "manifest.json"),
            "{\"gapMs\":0,\"clips\":{\"GROUND\":\"GROUND.wav\",\"OPEN\":\"OPEN.wav\",\"Ground is open\":\"go.wav\"}}");

        var result = new WavPackService().Build("GROUND IS OPEN.", _dir);

        Assert.NotNull(result.Pcm);
        Assert.Equal((960 * 5) + (500 * 48), result.Pcm!.Length / 2); // phrase clip + "." pause, not two word clips
    }

    [Fact]
    public void Build_UsesCommaVariantBeforeComma()
    {
        var plain = new short[960];
        var variant = new short[960 * 2];
        File.WriteAllBytes(Path.Combine(_dir, "a.wav"), MakeWav(48000, 1, 16, plain));
        File.WriteAllBytes(Path.Combine(_dir, "b.wav"), MakeWav(48000, 1, 16, variant));
        File.WriteAllText(Path.Combine(_dir, "manifest.json"),
            "{\"gapMs\":0,\"pauseMs\":{},\"clips\":{\"ONE\":\"a.wav\",\"ONE,\":\"b.wav\"}}");

        var svc = new WavPackService();
        Assert.Equal(960 * 2 * 2, svc.Build("ONE,", _dir).Pcm!.Length);
        Assert.Equal(960 * 2, svc.Build("ONE", _dir).Pcm!.Length);
    }

    [Fact]
    public void Importer_ConvertsLegacyAtisTxt()
    {
        foreach (var f in new[] { "1.wav", "A.wav", "9,.wav", "egll.wav", "SHORTSILENCE.WAV", "go.wav" })
        {
            File.WriteAllBytes(Path.Combine(_dir, f), MakeWav(48000, 1, 16, new short[4800]));
        }

        File.WriteAllText(Path.Combine(_dir, "ATIS.txt"),
            "RECORD:1:1.wav\nRECORD:A:A.wav\nRECORD:9,:9,.wav\nRECORD:Heathrow:egll.wav\n" +
            "RECORD:,:SHORTSILENCE.WAV\nRECORD:Ground is Open:go.wav\nRECORD:Gone:missing.wav\nRECORD:A_:A.wav\nITEM:Heathrow:XXX\n");

        var report = WavPackImporter.Import(_dir);

        Assert.Equal(["Gone (file not found: missing.wav)", "A_ (no vATIS equivalent)"], report.Skipped);
        var manifest = System.Text.Json.JsonSerializer.Deserialize<WavPackManifest>(
            File.ReadAllText(Path.Combine(_dir, "manifest.json")))!;
        Assert.Equal("1.wav", manifest.Clips["ONE"]);
        Assert.Equal("A.wav", manifest.Clips["ALPHA"]);
        Assert.Equal("9,.wav", manifest.Clips["NINER,"]);
        Assert.Equal("go.wav", manifest.Clips["GROUND IS OPEN"]);
        Assert.Equal(100, manifest.PauseMs[","]);
        Assert.Null(new WavPackService().Build("ONE ALPHA", _dir).MissingTokens.FirstOrDefault());
    }

    [Fact]
    public void Build_FolderWithTrailingSeparator_Works()
    {
        WritePack(("ONE", new short[960]));
        var result = new WavPackService().Build("ONE", _dir + Path.DirectorySeparatorChar);
        Assert.NotNull(result.Pcm);
        Assert.Empty(new WavPackService().Validate(_dir + Path.DirectorySeparatorChar).Problems);
    }

    [Fact]
    public void Build_NoManifest_Throws()
    {
        Assert.Throws<WavPackException>(() => new WavPackService().Build("ONE", _dir));
    }

    [Fact]
    public void AtisVoiceMeta_Clone_CopiesWavPackSettings()
    {
        var clone = new AtisVoiceMeta { UseTextToSpeech = false, UseWavPack = true, WavPackPath = "/x" }.Clone();
        Assert.True(clone.UseWavPack);
        Assert.Equal("/x", clone.WavPackPath);
        Assert.True(clone.IsAutomatic);
    }

    private void WritePack(params (string Token, short[] Samples)[] clips)
    {
        var entries = new StringBuilder();
        foreach (var (token, samples) in clips)
        {
            File.WriteAllBytes(Path.Combine(_dir, token + ".wav"), MakeWav(48000, 1, 16, samples));
            entries.Append($"\"{token}\":\"{token}.wav\",");
        }

        File.WriteAllText(Path.Combine(_dir, "manifest.json"), "{\"gapMs\":20,\"clips\":{" + entries.ToString().TrimEnd(',') + "}}");
    }

    private static byte[] MakeWav(int rate, int channels, int bits, short[] samples)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        var dataLen = samples.Length * 2;
        w.Write("RIFF"u8.ToArray());
        w.Write(36 + dataLen);
        w.Write("WAVEfmt "u8.ToArray());
        w.Write(16);
        w.Write((short)1);
        w.Write((short)channels);
        w.Write(rate);
        w.Write(rate * channels * bits / 8);
        w.Write((short)(channels * bits / 8));
        w.Write((short)bits);
        w.Write("data"u8.ToArray());
        w.Write(dataLen);
        foreach (var s in samples)
        {
            w.Write(s);
        }

        return ms.ToArray();
    }
}
