using Android.Media;

namespace RemoControlMobile;

public static partial class PlatformAudio
{
    public static partial async Task<byte[]> RecordWavAsync(TimeSpan duration)
    {
        PermissionStatus permiso = await Permissions.RequestAsync<Permissions.Microphone>();
        if (permiso != PermissionStatus.Granted)
            throw new InvalidOperationException("Debes permitir el uso del micrófono para hablar a la PC.");

        const int sampleRate = 16000;
        ChannelIn channel = ChannelIn.Mono;
        Encoding encoding = Encoding.Pcm16bit;
        int min = AudioRecord.GetMinBufferSize(sampleRate, channel, encoding);
        int bufferSize = Math.Max(min, 4096);

        using AudioRecord recorder = new AudioRecord(
            AudioSource.Mic,
            sampleRate,
            channel,
            encoding,
            bufferSize);

        using MemoryStream pcm = new MemoryStream();
        byte[] buffer = new byte[bufferSize];
        DateTime fin = DateTime.UtcNow.Add(duration);

        recorder.StartRecording();
        try
        {
            while (DateTime.UtcNow < fin)
            {
                int read = recorder.Read(buffer, 0, buffer.Length);
                if (read > 0)
                    pcm.Write(buffer, 0, read);
                await Task.Yield();
            }
        }
        finally
        {
            recorder.Stop();
        }

        return BuildWav(pcm.ToArray(), sampleRate, 1, 16);
    }

    public static partial Task PlayWavAsync(byte[] wavData)
    {
        if (!EsWavValido(wavData))
            throw new InvalidOperationException("La PC no devolvió audio WAV válido.");

        return Task.Run(async () =>
        {
            string file = Path.Combine(FileSystem.CacheDirectory, $"remocontrol_rx_{Guid.NewGuid():N}.wav");
            File.WriteAllBytes(file, wavData);

            try
            {
                using MediaPlayer player = new MediaPlayer();
                player.SetAudioStreamType(Android.Media.Stream.Music);
                player.SetDataSource(file);
                player.Prepare();
                player.Start();

                while (player.IsPlaying)
                    await Task.Delay(60);
            }
            finally
            {
                try { File.Delete(file); } catch { }
            }
        });
    }
}
