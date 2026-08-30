namespace RemoControlMobile;

public static partial class PlatformAudio
{
    public static partial Task<byte[]> RecordWavAsync(TimeSpan duration);
    public static partial Task PlayWavAsync(byte[] wavData);

    public static bool EsWavValido(byte[] data)
    {
        return data.Length >= 44 &&
               data[0] == (byte)'R' &&
               data[1] == (byte)'I' &&
               data[2] == (byte)'F' &&
               data[3] == (byte)'F' &&
               data[8] == (byte)'W' &&
               data[9] == (byte)'A' &&
               data[10] == (byte)'V' &&
               data[11] == (byte)'E';
    }

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
