using System.Net.Http.Headers;
using System.Text;

namespace RemoControlMobile;

public partial class IntercomPage : ContentPage
{
    private bool ocupado;
    private CancellationTokenSource? audioLiveCts;
    private CancellationTokenSource? cameraCts;
    private bool audioLiveActivo;
    private bool cameraActiva;

    public IntercomPage()
    {
        InitializeComponent();
        btnHablar.Text = $"Grabar {AppConfig.DuracionHablarSegundos} s y enviar";
        btnEscuchar.Text = $"Escuchar {Math.Max(3, AppConfig.DuracionHablarSegundos)} segundos";
    }

    private string Url(string path) =>
        AppConfig.Servidor.TrimEnd('/') + path;

    private async void BtnHablar_Clicked(object sender, EventArgs e)
    {
        if (ocupado || audioLiveActivo)
            return;

        ocupado = true;
        ActualizarControles();

        try
        {
            int segundos = AppConfig.DuracionHablarSegundos;
            lblEstadoAudio.Text = $"Grabando {segundos} segundos...";
            byte[] wav = await PlatformAudio.RecordWavAsync(TimeSpan.FromSeconds(segundos));
            lblEstadoAudio.Text = "Enviando audio a la PC...";

            using HttpClient http = AppConfig.CrearCliente(20);
            using ByteArrayContent content = new ByteArrayContent(wav);
            content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

            using HttpResponseMessage response =
                await http.PostAsync(Url("/audio/play"), content);

            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(body)
                        ? "La PC rechazó el audio."
                        : ExtraerError(body));

            lblEstadoAudio.Text = "Audio enviado correctamente.";
        }
        catch (Exception ex)
        {
            lblEstadoAudio.Text = "No se pudo enviar el audio.";
            await DisplayAlertAsync("Intercomunicador", ex.Message, "Aceptar");
        }
        finally
        {
            ocupado = false;
            ActualizarControles();
        }
    }

    private async void BtnEscuchar_Clicked(object sender, EventArgs e)
    {
        if (ocupado || audioLiveActivo)
            return;

        ocupado = true;
        ActualizarControles();

        try
        {
            int segundos = Math.Max(3, AppConfig.DuracionHablarSegundos);
            lblEstadoAudio.Text = $"Solicitando {segundos} segundos de audio...";
            byte[] wav = await ObtenerAudioPcAsync(segundos, CancellationToken.None);
            lblEstadoAudio.Text = "Reproduciendo audio de la PC...";
            await PlatformAudio.PlayWavAsync(wav);
            lblEstadoAudio.Text = "Listo";
        }
        catch (Exception ex)
        {
            lblEstadoAudio.Text = "No se pudo escuchar la PC.";
            await DisplayAlertAsync("Intercomunicador", ex.Message, "Aceptar");
        }
        finally
        {
            ocupado = false;
            ActualizarControles();
        }
    }

    private async void BtnEscucharVivo_Clicked(object sender, EventArgs e)
    {
        if (audioLiveActivo)
        {
            DetenerAudioLive();
            return;
        }

        if (ocupado)
            return;

        audioLiveActivo = true;
        audioLiveCts = new CancellationTokenSource();
        badgeEnVivo.IsVisible = true;
        btnEscucharVivo.Text = "Detener escucha en vivo";
        lblEstadoAudio.Text = "Conectando audio en vivo...";
        ActualizarControles();

        try
        {
            CancellationToken token = audioLiveCts.Token;
            int bloque = AppConfig.AudioLiveSegundos;
            byte[] actual = await ObtenerAudioPcAsync(bloque, token);

            while (!token.IsCancellationRequested)
            {
                lblEstadoAudio.Text = $"Escuchando PC en vivo • latencia aproximada {bloque} s";

                Task<byte[]> siguiente = ObtenerAudioPcAsync(bloque, token);
                await PlatformAudio.PlayWavAsync(actual);

                if (token.IsCancellationRequested)
                    break;

                actual = await siguiente;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (audioLiveActivo)
                await DisplayAlertAsync("Audio en vivo", ex.Message, "Aceptar");
        }
        finally
        {
            DetenerAudioLive();
            lblEstadoAudio.Text = "Audio en vivo detenido.";
        }
    }

    private async Task<byte[]> ObtenerAudioPcAsync(int segundos, CancellationToken token)
    {
        using HttpClient http = AppConfig.CrearCliente(segundos + 12);
        using HttpResponseMessage response = await http.GetAsync(
            Url("/audio/environment?seconds=" + segundos),
            HttpCompletionOption.ResponseContentRead,
            token);

        byte[] data = await response.Content.ReadAsByteArrayAsync(token);

        if (!response.IsSuccessStatusCode)
        {
            string text = Encoding.UTF8.GetString(data);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text)
                    ? "La PC rechazó la solicitud de audio."
                    : ExtraerError(text));
        }

        return data;
    }

    private async void BtnCamara_Clicked(object sender, EventArgs e)
    {
        if (cameraActiva)
        {
            DetenerCamara();
            return;
        }

        cameraActiva = true;
        cameraCts = new CancellationTokenSource();
        badgeCamara.IsVisible = true;
        btnCamara.Text = "Detener cámara";
        camaraPlaceholder.IsVisible = false;
        imgCamara.IsVisible = true;
        lblEstadoAudio.Text = "Conectando con la cámara de la PC...";

        try
        {
            CancellationToken token = cameraCts.Token;

            while (!token.IsCancellationRequested)
            {
                using HttpClient http = AppConfig.CrearCliente(10);
                using HttpResponseMessage response = await http.GetAsync(
                    Url("/camera/frame?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                    HttpCompletionOption.ResponseContentRead,
                    token);

                byte[] bytes = await response.Content.ReadAsByteArrayAsync(token);

                if (!response.IsSuccessStatusCode)
                {
                    string text = Encoding.UTF8.GetString(bytes);
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(text)
                            ? "La PC rechazó la cámara remota."
                            : ExtraerError(text));
                }

                byte[] frame = bytes;
                imgCamara.Source = ImageSource.FromStream(
                    () => new MemoryStream(frame, writable: false));

                lblEstadoAudio.Text = "Cámara de la PC • en vivo";
                await Task.Delay(550, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (cameraActiva)
                await DisplayAlertAsync("Cámara de la PC", ex.Message, "Aceptar");
        }
        finally
        {
            DetenerCamara();
        }
    }

    private void DetenerAudioLive()
    {
        audioLiveActivo = false;
        try { audioLiveCts?.Cancel(); } catch { }
        audioLiveCts?.Dispose();
        audioLiveCts = null;
        badgeEnVivo.IsVisible = false;
        btnEscucharVivo.Text = "Iniciar escucha en vivo";
        ActualizarControles();
    }

    private void DetenerCamara()
    {
        cameraActiva = false;
        try { cameraCts?.Cancel(); } catch { }
        cameraCts?.Dispose();
        cameraCts = null;
        badgeCamara.IsVisible = false;
        btnCamara.Text = "Iniciar cámara";
        imgCamara.Source = null;
        imgCamara.IsVisible = false;
        camaraPlaceholder.IsVisible = true;
    }

    private void ActualizarControles()
    {
        btnHablar.IsEnabled = !ocupado && !audioLiveActivo;
        btnEscuchar.IsEnabled = !ocupado && !audioLiveActivo;
        btnEscucharVivo.IsEnabled = !ocupado || audioLiveActivo;
    }

    protected override void OnDisappearing()
    {
        DetenerAudioLive();
        DetenerCamara();
        base.OnDisappearing();
    }

    private static string ExtraerError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "Error desconocido.";

        try
        {
            using System.Text.Json.JsonDocument doc =
                System.Text.Json.JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("error", out var error))
                return error.GetString() ?? body;
        }
        catch
        {
        }

        return body;
    }

    private async void BtnVolver_Clicked(object sender, EventArgs e)
    {
        DetenerAudioLive();
        DetenerCamara();
        await Navigation.PopAsync();
    }
}
