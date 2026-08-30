using AVFoundation;
using Foundation;

namespace RemoControlMobile;

public static partial class PlatformAudio
{
    public static partial async Task<byte[]> RecordWavAsync(TimeSpan duration)
    {
        PermissionStatus permiso = await Permissions.RequestAsync<Permissions.Microphone>();
        if (permiso != PermissionStatus.Granted)
            throw new InvalidOperationException("Debes permitir el uso del micrófono para hablar a la PC.");

        AVAudioSession session = AVAudioSession.SharedInstance();
        session.SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.DefaultToSpeaker);
        session.SetActive(true);

        string path = Path.Combine(FileSystem.CacheDirectory, "remocontrol_tx.wav");
        NSUrl url = NSUrl.FromFilename(path);
        NSDictionary settings = NSDictionary.FromObjectsAndKeys(
            new NSObject[]
            {
                NSNumber.FromInt32((int)AudioToolbox.AudioFormatType.LinearPCM),
                NSNumber.FromFloat(16000),
                NSNumber.FromInt32(1),
                NSNumber.FromInt32(16),
                NSNumber.FromBoolean(false),
                NSNumber.FromBoolean(false)
            },
            new NSObject[]
            {
                AVAudioSettings.AVFormatIDKey,
                AVAudioSettings.AVSampleRateKey,
                AVAudioSettings.AVNumberOfChannelsKey,
                AVAudioSettings.AVLinearPCMBitDepthKey,
                AVAudioSettings.AVLinearPCMIsBigEndianKey,
                AVAudioSettings.AVLinearPCMIsFloatKey
            });

        NSError? error;
        using AVAudioRecorder recorder = AVAudioRecorder.Create(url, new AudioSettings(settings), out error);
        if (recorder == null || error != null)
            throw new InvalidOperationException(error?.LocalizedDescription ?? "No se pudo abrir el micrófono.");

        recorder.Record();
        await Task.Delay(duration);
        recorder.Stop();
        return await File.ReadAllBytesAsync(path);
    }

    public static partial Task PlayWavAsync(byte[] wavData)
    {
        if (!EsWavValido(wavData))
            throw new InvalidOperationException("La PC no devolvió audio WAV válido.");

        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            AVAudioSession session = AVAudioSession.SharedInstance();
            session.SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.DefaultToSpeaker);
            session.SetActive(true);

            string path = Path.Combine(FileSystem.CacheDirectory, $"remocontrol_rx_{Guid.NewGuid():N}.wav");
            await File.WriteAllBytesAsync(path, wavData);

            AVAudioPlayer? player = null;
            try
            {
                player = AVAudioPlayer.FromUrl(NSUrl.FromFilename(path));
                if (player == null)
                    throw new InvalidOperationException("No se pudo abrir el audio recibido.");

                player.PrepareToPlay();
                if (!player.Play())
                    throw new InvalidOperationException("No se pudo reproducir el audio recibido.");

                while (player.Playing)
                    await Task.Delay(60);
            }
            finally
            {
                player?.Dispose();
                try { File.Delete(path); } catch { }
            }
        });
    }
}
