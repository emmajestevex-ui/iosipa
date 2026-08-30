using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

    private async void BtnPermisosCamara_Clicked(object sender, EventArgs e)
    {
        try
        {
            using HttpClient http = AppConfig.CrearCliente(8);
            await PostFirstAsync(
                http,
                "/pro/privacy?target=camera",
                "/pro/settings?target=camera",
                "/camera/permissions");

            lblEstadoAudio.Text = "Solicitud enviada a la PC.";
        }
        catch (Exception ex)
        {
            lblEstadoAudio.Text = "No se pudieron abrir los permisos.";
            await DisplayAlertAsync("Permisos de cámara", ex.Message, "Aceptar");
        }
    }

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
            string text = LeerTextoSeguro(data);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text)
                    ? "La PC rechazó la solicitud de audio."
                    : ExtraerError(text));
        }

        if (!PlatformAudio.EsWavValido(data))
        {
            string text = LeerTextoSeguro(data);
            string mediaType = response.Content.Headers.ContentType?.MediaType ?? "sin tipo";
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text)
                    ? $"La PC no devolvió audio WAV válido ({mediaType}). Revisa que RemoControl PC tenga permiso de micrófono."
                    : ExtraerError(text));
        }

        return data;
    }

    private async Task<byte[]> ObtenerFrameCamaraAsync(HttpClient http, CancellationToken token)
    {
        using HttpResponseMessage response = await http.GetAsync(
            Url("/camera/frame?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            HttpCompletionOption.ResponseContentRead,
            token);

        byte[] bytes = await response.Content.ReadAsByteArrayAsync(token);

        if (!response.IsSuccessStatusCode)
        {
            string text = LeerTextoSeguro(bytes);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text)
                    ? "La PC rechazó la cámara remota."
                    : ExtraerError(text));
        }

        if (TieneFirmaImagen(bytes))
            return bytes;

        byte[]? imagenJson = IntentarLeerImagenDesdeJson(bytes);
        if (imagenJson != null)
            return imagenJson;

        string detalle = LeerTextoSeguro(bytes);
        if (!string.IsNullOrWhiteSpace(detalle))
            throw new InvalidOperationException(ExtraerError(detalle));

        string mediaType = response.Content.Headers.ContentType?.MediaType ?? "sin tipo";
        throw new InvalidOperationException(
            $"La PC no devolvió una imagen válida ({mediaType}). Activa la cámara en RemoControl PC y permite el acceso de cámara en Windows.");
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
        badgeCamara.IsVisible = false;
        btnCamara.Text = "Detener cámara";
        camaraPlaceholder.IsVisible = true;
        imgCamara.Source = null;
        imgCamara.IsVisible = false;
        lblEstadoAudio.Text = "Conectando con la cámara de la PC...";

        try
        {
            CancellationToken token = cameraCts.Token;
            using HttpClient http = AppConfig.CrearCliente(10);

            while (!token.IsCancellationRequested)
            {
                byte[] frame = await ObtenerFrameCamaraAsync(http, token);
                imgCamara.Source = ImageSource.FromStream(
                    () => new MemoryStream(frame, writable: false));

                if (!imgCamara.IsVisible)
                {
                    camaraPlaceholder.IsVisible = false;
                    imgCamara.IsVisible = true;
                    badgeCamara.IsVisible = true;
                }

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

    private static bool TieneFirmaImagen(byte[] data)
    {
        if (data.Length < 4)
            return false;

        bool jpg = data.Length >= 3 &&
                   data[0] == 0xFF &&
                   data[1] == 0xD8 &&
                   data[2] == 0xFF;

        bool png = data.Length >= 8 &&
                   data[0] == 0x89 &&
                   data[1] == 0x50 &&
                   data[2] == 0x4E &&
                   data[3] == 0x47 &&
                   data[4] == 0x0D &&
                   data[5] == 0x0A &&
                   data[6] == 0x1A &&
                   data[7] == 0x0A;

        bool gif = data.Length >= 6 &&
                   data[0] == (byte)'G' &&
                   data[1] == (byte)'I' &&
                   data[2] == (byte)'F';

        bool bmp = data[0] == (byte)'B' &&
                   data[1] == (byte)'M';

        bool webp = data.Length >= 12 &&
                    data[0] == (byte)'R' &&
                    data[1] == (byte)'I' &&
                    data[2] == (byte)'F' &&
                    data[3] == (byte)'F' &&
                    data[8] == (byte)'W' &&
                    data[9] == (byte)'E' &&
                    data[10] == (byte)'B' &&
                    data[11] == (byte)'P';

        return jpg || png || gif || bmp || webp;
    }

    private static byte[]? IntentarLeerImagenDesdeJson(byte[] data)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(data);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String ||
                    !EsCampoImagen(property.Name))
                {
                    continue;
                }

                string value = property.Value.GetString() ?? "";
                int comma = value.IndexOf(',');
                if (value.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                    value = value[(comma + 1)..];

                byte[] decoded = Convert.FromBase64String(value);
                if (TieneFirmaImagen(decoded))
                    return decoded;
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool EsCampoImagen(string name)
    {
        return name.Contains("image", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("imagen", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("frame", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("jpg", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("png", StringComparison.OrdinalIgnoreCase);
    }

    private static string LeerTextoSeguro(byte[] data)
    {
        if (data.Length == 0 || PlatformAudio.EsWavValido(data) || TieneFirmaImagen(data))
            return "";

        string text = Encoding.UTF8.GetString(data).Trim();
        return text.Length <= 600 ? text : text[..600];
    }

    private async Task PostFirstAsync(HttpClient http, params string[] rutas)
    {
        string ultimoError = "La PC no respondió.";

        foreach (string ruta in rutas.Distinct())
        {
            try
            {
                using HttpResponseMessage response =
                    await http.PostAsync(Url(ruta), null);

                string body =
                    await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return;

                ultimoError = ExtraerError(body);
            }
            catch (Exception ex)
            {
                ultimoError = ex.Message;
            }
        }

        throw new InvalidOperationException(ultimoError);
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
