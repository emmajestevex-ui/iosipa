namespace RemoControlMobile;

public partial class TecladoPage : ContentPage
{
    public TecladoPage()
    {
        InitializeComponent();
    }


    // ============================================================
    // ENVIAR PETICIÓN
    // ============================================================

    private async Task Post(
        string endpoint)
    {
        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(8);

            using HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    AppConfig.Servidor +
                    endpoint,
                    null);

            if (respuesta.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "● Ejecutado";

                lblEstado.TextColor =
                    Colors.LimeGreen;
            }
            else
            {
                lblEstado.Text =
                    "● Error";

                lblEstado.TextColor =
                    Colors.Red;
            }
        }
        catch
        {
            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;
        }
    }


    // ============================================================
    // ENVIAR TEXTO
    // ============================================================

    private async void BtnEnviarTexto_Clicked(
        object sender,
        EventArgs e)
    {
        string texto =
            txtTexto.Text ?? "";

        if (string.IsNullOrWhiteSpace(
            texto))
        {
            lblEstado.Text =
                "Escribe algo primero";

            lblEstado.TextColor =
                Colors.Orange;

            return;
        }

        await Post(
            "/input/text?text=" +
            Uri.EscapeDataString(
                texto));
    }


    // ============================================================
    // TECLAS
    // ============================================================

    private Task Tecla(
        string tecla)
    {
        return Post(
            "/input/key?key=" +
            Uri.EscapeDataString(
                tecla));
    }


    private async void BtnEnter_Clicked(
        object sender,
        EventArgs e)
    {
        await Tecla("enter");
    }


    private async void BtnBackspace_Clicked(
        object sender,
        EventArgs e)
    {
        await Tecla("backspace");
    }


    private async void BtnTab_Clicked(
        object sender,
        EventArgs e)
    {
        await Tecla("tab");
    }


    private async void BtnEsc_Clicked(
        object sender,
        EventArgs e)
    {
        await Tecla("esc");
    }


    private async void BtnDelete_Clicked(
        object sender,
        EventArgs e)
    {
        await Tecla("delete");
    }


    private async void BtnArriba_Clicked(
        object sender,
        EventArgs e)
    {
        await Tecla("up");
    }


    private async void BtnAbajo_Clicked(
        object sender,
        EventArgs e)
    {
        await Tecla("down");
    }


    private async void BtnIzquierda_Clicked(
        object sender,
        EventArgs e)
    {
        await Tecla("left");
    }


    private async void BtnDerecha_Clicked(
        object sender,
        EventArgs e)
    {
        await Tecla("right");
    }


    // ============================================================
    // ATAJOS
    // ============================================================

    private Task Hotkey(
        string accion)
    {
        return Post(
            "/input/hotkey?action=" +
            Uri.EscapeDataString(
                accion));
    }


    private async void BtnCopiar_Clicked(
        object sender,
        EventArgs e)
    {
        await Hotkey("copy");
    }


    private async void BtnPegar_Clicked(
        object sender,
        EventArgs e)
    {
        await Hotkey("paste");
    }


    private async void BtnSeleccionarTodo_Clicked(
        object sender,
        EventArgs e)
    {
        await Hotkey("selectall");
    }


    private async void BtnCortar_Clicked(
        object sender,
        EventArgs e)
    {
        await Hotkey("cut");
    }


    private async void BtnDeshacer_Clicked(
        object sender,
        EventArgs e)
    {
        await Hotkey("undo");
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