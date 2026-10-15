// <copyright file="WavDecoder.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Buffers.Binary;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// Decodes PCM WAV data into 48 kHz mono signed 16-bit little-endian samples.
/// </summary>
public static class WavDecoder
{
    /// <summary>
    /// The sample rate of audio sent to the voice server.
    /// </summary>
    public const int TargetSampleRate = 48000;

    /// <summary>
    /// Decodes a PCM WAV file (8, 16, 24 or 32-bit integer; any channel count) to 48 kHz mono 16-bit samples.
    /// </summary>
    /// <param name="wav">The raw WAV file bytes.</param>
    /// <returns>The decoded samples.</returns>
    /// <exception cref="InvalidOperationException">The data is not a supported PCM WAV file.</exception>
    public static short[] Decode(ReadOnlySpan<byte> wav)
    {
        if (wav.Length < 12 || !wav[..4].SequenceEqual("RIFF"u8) || !wav.Slice(8, 4).SequenceEqual("WAVE"u8))
        {
            throw new InvalidOperationException("Not a RIFF/WAVE file.");
        }

        int channels = 0, sampleRate = 0, bits = 0, format = 0;
        ReadOnlySpan<byte> data = default;
        var haveFmt = false;
        var haveData = false;

        var pos = 12;
        while (pos + 8 <= wav.Length)
        {
            var id = wav.Slice(pos, 4);
            var size = (long)BinaryPrimitives.ReadUInt32LittleEndian(wav.Slice(pos + 4, 4));
            var bodyStart = pos + 8;
            var available = Math.Min(size, wav.Length - bodyStart);

            if (id.SequenceEqual("fmt "u8) && available >= 16)
            {
                var fmt = wav.Slice(bodyStart, (int)available);
                format = BinaryPrimitives.ReadUInt16LittleEndian(fmt[..2]);
                channels = BinaryPrimitives.ReadUInt16LittleEndian(fmt.Slice(2, 2));
                sampleRate = (int)BinaryPrimitives.ReadUInt32LittleEndian(fmt.Slice(4, 4));
                bits = BinaryPrimitives.ReadUInt16LittleEndian(fmt.Slice(14, 2));
                if (format == 0xFFFE && fmt.Length >= 26)
                {
                    format = BinaryPrimitives.ReadUInt16LittleEndian(fmt.Slice(24, 2)); // extensible sub-format
                }

                haveFmt = true;
            }
            else if (id.SequenceEqual("data"u8))
            {
                data = wav.Slice(bodyStart, (int)available);
                haveData = true;
            }

            pos = (int)Math.Min(int.MaxValue, bodyStart + size + (size & 1));
        }

        if (!haveFmt || !haveData)
        {
            throw new InvalidOperationException("WAV file is missing a fmt or data chunk.");
        }

        if (format != 1)
        {
            throw new InvalidOperationException("Only uncompressed PCM WAV files are supported.");
        }

        if (channels < 1 || sampleRate < 1 || bits is not (8 or 16 or 24 or 32))
        {
            throw new InvalidOperationException("Unsupported WAV format.");
        }

        var bytesPerSample = bits / 8;
        var frameSize = bytesPerSample * channels;
        var frames = data.Length / frameSize;
        var mono = new float[frames];

        for (var i = 0; i < frames; i++)
        {
            float sum = 0;
            for (var c = 0; c < channels; c++)
            {
                sum += ReadSample(data.Slice((i * frameSize) + (c * bytesPerSample), bytesPerSample), bits);
            }

            mono[i] = sum / channels;
        }

        return Resample(mono, sampleRate);
    }

    private static float ReadSample(ReadOnlySpan<byte> s, int bits)
    {
        return bits switch
        {
            8 => (s[0] - 128) / 128f,
            16 => BinaryPrimitives.ReadInt16LittleEndian(s) / 32768f,
            24 => (((s[2] << 24) | (s[1] << 16) | (s[0] << 8)) >> 8) / 8388608f,
            _ => BinaryPrimitives.ReadInt32LittleEndian(s) / 2147483648f
        };
    }

    private static short[] Resample(float[] input, int sourceRate)
    {
        if (input.Length == 0)
        {
            return [];
        }

        var outLength = sourceRate == TargetSampleRate
            ? input.Length
            : (int)((long)input.Length * TargetSampleRate / sourceRate);
        var output = new short[outLength];
        var ratio = (double)sourceRate / TargetSampleRate;

        for (var i = 0; i < outLength; i++)
        {
            float value;
            if (sourceRate == TargetSampleRate)
            {
                value = input[i];
            }
            else
            {
                var src = i * ratio;
                var idx = (int)src;
                var frac = (float)(src - idx);
                var next = Math.Min(idx + 1, input.Length - 1);
                value = (input[idx] * (1 - frac)) + (input[next] * frac);
            }

            output[i] = (short)Math.Clamp(MathF.Round(value * 32767f), short.MinValue, short.MaxValue);
        }

        return output;
    }
}
