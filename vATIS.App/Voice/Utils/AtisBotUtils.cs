using Concentus;
using System;
using System.Collections.Generic;
using Concentus.Enums;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis.Voice.Utils;

public static class AtisBotUtils
{
	private const int FRAME_SIZE = 960;
	private const int SAMPLE_RATE = 48000;
	private const double BYTES_PER_SECOND = 96000.0;
	private const int BIT_RATE = 8192;
	private const int MAX_OPUS_PACKET_LENGTH = 1275;
	private static readonly byte[] EncodedBuffer = new byte[MAX_OPUS_PACKET_LENGTH];
	public static PutBotRequestDto AddBotRequest(byte[] audioData, uint frequency, double latDeg, double lonDeg,
		double altM)
	{
		var audioBuffer = ConvertBytesTo16BitPcm(audioData);
		
		var encoder = OpusCodecFactory.CreateEncoder(SAMPLE_RATE, 1, OpusApplication.OPUS_APPLICATION_VOIP);
		encoder.Bitrate = BIT_RATE;
		
		Array.Clear(EncodedBuffer, 0, EncodedBuffer.Length);

		var segmentCount = (int)Math.Floor((double)audioBuffer.Length / FRAME_SIZE);
		var bufferOffset = 0;
		List<byte[]> opusData = [];

		for (var i = 0; i < segmentCount; i++)
		{
			var pcmSegment = new ReadOnlySpan<short>(audioBuffer, bufferOffset, FRAME_SIZE);
			Span<byte> outputBuffer = EncodedBuffer;

			var len = encoder.Encode(pcmSegment, FRAME_SIZE, outputBuffer, EncodedBuffer.Length);
			var trimmedBuffer = new byte[len];
			Buffer.BlockCopy(EncodedBuffer, 0, trimmedBuffer, 0, len);
			opusData.Add(trimmedBuffer);

			bufferOffset += FRAME_SIZE;
		}

		return new PutBotRequestDto
		{
			Transceivers =
			[
				new TransceiverDto
				{
					ID = 0,
					Frequency = frequency,
					LatDeg = latDeg,
					LonDeg = lonDeg,
					HeightAglM = altM,
					HeightMslM = altM
				}
			],
			Interval = TimeSpan.FromSeconds(audioData.Length / BYTES_PER_SECOND + 3.0),
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