namespace RemoControlMobile;

public partial class AccionesPage : ContentPage
{
    private readonly HttpClient cliente =
        AppConfig.CrearCliente(8);

    public AccionesPage()
    {
        InitializeComponent();
    }

    private async Task EjecutarAccion(
        string accion)
    {
        try
        {
            lblEstado.Text =
                "Enviando...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpResponseMessage respuesta =
                await AppConfig.PostAsyncConToken(
                    cliente,
                    "/action/" +
                    accion);

            lblEstado.Text =
                respuesta.IsSuccessStatusCode
                ? "● Acción enviada"
                : "● Error";

            lblEstado.TextColor =
                respuesta.IsSuccessStatusCode
                ? Colors.LimeGreen
                : Colors.Red;
        }
        catch
        {
            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;
        }
    }

    private async void BtnBloquear_Clicked(
        object sender,
        EventArgs e)
    {
        await EjecutarAccion(
            "lock");
    }

    private async void BtnSuspender_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar =
            await DisplayAlertAsync(
                "Suspender",
                "¿Suspender la PC?",
                "Sí",
                "Cancelar");

        if (confirmar)
        {
            await EjecutarAccion(
                "sleep");
        }
    }

    private async void BtnReiniciar_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar =
            await DisplayAlertAsync(
                "Reiniciar",
                "¿Reiniciar la PC?",
                "Sí",
                "Cancelar");

        if (confirmar)
        {
            await EjecutarAccion(
                "restart");
        }
    }

    private async void BtnApagar_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar =
            await DisplayAlertAsync(
                "Apagar",
                "¿Apagar la PC?",
                "Sí",
                "Cancelar");

        if (confirmar)
        {
            await EjecutarAccion(
                "shutdown");
        }
    }

    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
