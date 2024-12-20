using System;
using System.Collections.Generic;
using System.IO;

namespace Vatsim.Vatis.Voice.Audio;

public class WaveFileBuilder
{
    private const int SAMPLE_RATE = 44100;
    private const int BITS_PER_SAMPLE = 16;
    private const int CHANNELS = 1;
    
    private readonly FileStream mFileStream;
    private readonly BinaryWriter mWriter;
    private long mDataChunkSizePosition;

    public WaveFileBuilder(string filePath)
    {
        // Open a FileStream and BinaryWriter for writing
        mFileStream = new FileStream(filePath, FileMode.Create);
        mWriter = new BinaryWriter(mFileStream);

        // Write the header with placeholder sizes
        WriteWaveHeader();
    }

    private void WriteWaveHeader()
    {
        mWriter.Write("RIFF"u8.ToArray());
        mWriter.Write(0); // Placeholder for chunk size, to be updated later
        mWriter.Write("WAVE"u8.ToArray());

        // fmt sub-chunk
        mWriter.Write("fmt "u8.ToArray());
        mWriter.Write(16); // Sub-chunk size (16 for PCM)
        mWriter.Write((short)1); // Audio format (1 for PCM)
        mWriter.Write((short)CHANNELS);
        mWriter.Write(SAMPLE_RATE);
        mWriter.Write(SAMPLE_RATE * CHANNELS * BITS_PER_SAMPLE / 8); // Byte rate
        mWriter.Write((short)(CHANNELS * BITS_PER_SAMPLE / 8));     // Block align
        mWriter.Write((short)BITS_PER_SAMPLE);

        // data sub-chunk
        mWriter.Write("data"u8.ToArray());
        mDataChunkSizePosition = mFileStream.Position;
        mWriter.Write(0); // Placeholder for data chunk size, to be updated later
    }

    public void AppendAudioData(byte[] audioData)
    {
        mWriter.Write(audioData);
    }

    public void Save()
    {
        // Update the data chunk size
        var fileSize = mFileStream.Position;

        mWriter.Seek((int)mDataChunkSizePosition, SeekOrigin.Begin);
        mWriter.Write((int)(fileSize - mDataChunkSizePosition - 4));

        // Update the RIFF chunk size
        mWriter.Seek(4, SeekOrigin.Begin);
        mWriter.Write((int)(fileSize - 8));

        mWriter.Flush();
        
        mWriter?.Dispose();
        mFileStream?.Dispose();
    }
    
    public static byte[] CombineAudioBuffers(List<byte[]> audioBuffers)
    {
        // Calculate the total length of the resulting byte array
        int totalLength = 0;
        foreach (var byteArray in audioBuffers)
        {
            totalLength += byteArray.Length;
        }

        // Create a new byte array to hold the combined data
        byte[] result = new byte[totalLength];
        int currentIndex = 0;

        // Copy each byte array into the result array
        foreach (var byteArray in audioBuffers)
        {
            Buffer.BlockCopy(byteArray, 0, result, currentIndex, byteArray.Length);
            currentIndex += byteArray.Length;
        }

        return result;
    }
}