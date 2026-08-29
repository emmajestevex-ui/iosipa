namespace RemoControlMobile;

public partial class MultimediaPage : ContentPage
{
    public MultimediaPage()
    {
        InitializeComponent();
    }


    // ============================================================
    // ENVIAR COMANDO
    // ============================================================

    private async Task EnviarComando(
        string comando,
        string textoEstado)
    {
        try
        {
            lblEstado.Text =
                "Enviando...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(
                    8);

            string url =
                AppConfig.Servidor +
                "/media?action=" +
                Uri.EscapeDataString(
                    comando);

            using HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    url,
                    null);

            if (!respuesta.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "No se pudo ejecutar.";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            lblEstado.Text =
                "● " +
                textoEstado;

            lblEstado.TextColor =
                Colors.LimeGreen;
        }
        catch
        {
            lblEstado.Text =
                "Sin conexión con la PC";

            lblEstado.TextColor =
                Colors.Red;
        }
    }


    // ============================================================
    // PLAY / PAUSA
    // ============================================================

    private async void BtnPlayPause_Clicked(
        object sender,
        EventArgs e)
    {
        await EnviarComando(
            "playpause",
            "Reproducir / Pausar");
    }


    // ============================================================
    // ANTERIOR
    // ============================================================

    private async void BtnAnterior_Clicked(
        object sender,
        EventArgs e)
    {
        await EnviarComando(
            "previous",
            "Pista anterior");
    }


    // ============================================================
    // SIGUIENTE
    // ============================================================

    private async void BtnSiguiente_Clicked(
        object sender,
        EventArgs e)
    {
        await EnviarComando(
            "next",
            "Pista siguiente");
    }


    // ============================================================
    // DETENER
    // ============================================================

    private async void BtnDetener_Clicked(
        object sender,
        EventArgs e)
    {
        await EnviarComando(
            "stop",
            "Reproducción detenida");
    }


    // ============================================================
    // VOLUMEN +
    // ============================================================

    private async void BtnVolumenMas_Clicked(
        object sender,
        EventArgs e)
    {
        await EnviarComando(
            "volumeup",
            "Volumen aumentado");
    }


    // ============================================================
    // VOLUMEN -
    // ============================================================

    private async void BtnVolumenMenos_Clicked(
        object sender,
        EventArgs e)
    {
        await EnviarComando(
            "volumedown",
            "Volumen reducido");
    }


    // ============================================================
    // SILENCIO
    // ============================================================

    private async void BtnMute_Clicked(
        object sender,
        EventArgs e)
    {
        await EnviarComando(
            "mute",
            "Silencio activado / desactivado");
    }


    // ============================================================
    // VOLVER
    // ============================================================

    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}