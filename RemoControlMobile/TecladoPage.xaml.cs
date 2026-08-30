namespace RemoControlMobile;

public partial class TecladoPage : ContentPage
{
    public TecladoPage()
    {
        InitializeComponent();
    }

    private string U(string endpoint)
    {
        return AppConfig.Url(
            endpoint);
    }

    // ============================================================
    // ENVIAR PETICIÓN
    // ============================================================

    private async Task<bool> Post(
        string endpoint)
    {
        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(8);

            using HttpResponseMessage respuesta =
                await AppConfig.PostAsyncConToken(
                    cliente,
                    endpoint);

            if (respuesta.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "● Ejecutado";

                lblEstado.TextColor =
                    Colors.LimeGreen;

                return true;
            }
            else
            {
                string detalle =
                    await respuesta.Content.ReadAsStringAsync();

                lblEstado.Text =
                    string.IsNullOrWhiteSpace(detalle)
                        ? "● Error"
                        : "● " + Recortar(detalle, 80);

                lblEstado.TextColor =
                    Colors.Red;

                return false;
            }
        }
        catch (Exception ex)
        {
            lblEstado.Text =
                "● " + Recortar(ex.Message, 80);

            lblEstado.TextColor =
                Colors.Red;

            return false;
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

    private async void BtnMacroJuego_Clicked(
        object sender,
        EventArgs e)
    {
        string texto =
            txtMacroJuego.Text?.Trim() ??
            "";

        if (string.IsNullOrWhiteSpace(texto))
        {
            texto =
                "Emmanuel";
        }

        lblEstado.Text =
            "● Ejecutando macro...";

        lblEstado.TextColor =
            Colors.Orange;

        if (!await Tecla("enter"))
            return;

        await Task.Delay(120);

        if (!await Post(
                "/input/text?text=" +
                Uri.EscapeDataString(
                    texto)))
        {
            return;
        }

        await Task.Delay(120);

        if (await Tecla("enter"))
        {
            lblEstado.Text =
                "● Macro ejecutada";

            lblEstado.TextColor =
                Colors.LimeGreen;
        }
    }


    // ============================================================
    // TECLAS
    // ============================================================

    private Task<bool> Tecla(
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

    private Task<bool> Hotkey(
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

    private static string Recortar(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Sin detalle.";

        text = text.Trim();
        return text.Length <= max ? text : text[..max];
    }
}
