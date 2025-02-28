// <copyright file="AtisBotUtils.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Concentus;
using Concentus.Enums;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis.Voice.Utils;

/// <summary>
/// Provides utility methods for handling operations related to ATIS bot functionality.
/// </summary>
public static class AtisBotUtils
{
    private const int FrameSize = 960;
    private const int SampleRate = 48000;
    private const double BytesPerSecond = 96000.0;
    private const int BitRate = 8192;
    private const int MaxOpusPacketLength = 1275;
    private static readonly byte[] s_encodedBuffer = new byte[MaxOpusPacketLength];

    /// <summary>
    /// Creates a <see cref="PutBotRequestDto"/> using the provided audio data and station properties for the bot.
    /// </summary>
    /// <param name="audioData">The audio data buffer to be processed.</param>
    /// <param name="frequency">The operating frequency for the bot.</param>
    /// <param name="latDeg">The latitude in degrees where the bot is located.</param>
    /// <param name="lonDeg">The longitude in degrees where the bot is located.</param>
    /// <param name="altM">The altitude in meters for the bot's position.</param>
    /// <returns>A new instance of the <see cref="PutBotRequestDto"/> containing the bot setup data.</returns>
    public static PutBotRequestDto CreateAtisBotDto(byte[] audioData, uint frequency, double latDeg, double lonDeg,
        double altM)
    {
        var audioBuffer = ConvertBytesTo16BitPcm(audioData);

        var encoder = OpusCodecFactory.CreateEncoder(SampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = BitRate;

        Array.Clear(s_encodedBuffer, 0, s_encodedBuffer.Length);

        var segmentCount = (int)Math.Floor((double)audioBuffer.Length / FrameSize);
        var bufferOffset = 0;
        List<byte[]> opusData = [];

        for (var i = 0; i < segmentCount; i++)
        {
            var pcmSegment = new ReadOnlySpan<short>(audioBuffer, bufferOffset, FrameSize);
            Span<byte> outputBuffer = s_encodedBuffer;

            var len = encoder.Encode(pcmSegment, FrameSize, outputBuffer, s_encodedBuffer.Length);
            var trimmedBuffer = new byte[len];
            Buffer.BlockCopy(s_encodedBuffer, 0, trimmedBuffer, 0, len);
            opusData.Add(trimmedBuffer);

            bufferOffset += FrameSize;
        }

        return new PutBotRequestDto
        {
            Transceivers =
            [
                new TransceiverDto
                {
                    Id = 0,
                    Frequency = frequency,
                    LatDeg = latDeg,
                    LonDeg = lonDeg,
                    HeightAglM = altM,
                    HeightMslM = altM
                }
            ],
            Interval = TimeSpan.FromSeconds((audioData.Length / BytesPerSecond) + 3.0),
            OpusData = opusData
        };
    }

    private static short[] ConvertBytesTo16BitPcm(byte[] input)
    {
        var inputSamples = input.Length / 2;
        var output = new short[inputSamples];
        var outputIndex = 0;
        for (var i = 0; i < inputSamples; i++)
        {
            output[outputIndex++] = BitConverter.ToInt16(input, i * 2);
        }

        return output;
    }
}
