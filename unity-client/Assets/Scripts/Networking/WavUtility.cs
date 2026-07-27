using System;
using System.IO;
using System.Text;

namespace EmoScape.Networking
{
    /// <summary>Converts recorded AudioClip samples into a 16-bit PCM WAV byte[] for upload to /stt.</summary>
    public static class WavUtility
    {
        public static byte[] FromSamples(float[] samples, int channels, int sampleRate)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            int dataSize = samples.Length * 2;
            int byteRate = sampleRate * channels * 2;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2)); // block align
            writer.Write((short)16); // bits per sample
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (var sample in samples)
            {
                short s = (short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
                writer.Write(s);
            }

            writer.Flush();
            return stream.ToArray();
        }
    }
}
