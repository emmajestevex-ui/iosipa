namespace RemoControlMobile;

public static partial class PlatformAudio
{
    public static partial Task<byte[]> RecordWavAsync(TimeSpan duration);
    public static partial Task PlayWavAsync(byte[] wavData);

    public static byte[] BuildWav(byte[] pcm, int sampleRate = 16000, short channels = 1, short bitsPerSample = 16)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter w = new BinaryWriter(ms);
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);
        w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + pcm.Length);
        w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        w.Write(16);
        w.Write((short)1);
        w.Write(channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write(blockAlign);
        w.Write(bitsPerSample);
        w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        w.Write(pcm.Length);
        w.Write(pcm);
        w.Flush();
        return ms.ToArray();
    }
}
