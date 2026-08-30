
using System.Net.Http.Json;

namespace RemoControlMobile;

public partial class SeguridadPage : ContentPage
{
    public SeguridadPage()
    {
        InitializeComponent();
    }


    // ============================================================
    // AL ABRIR LA PÁGINA
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarEstado();
    }


    // ============================================================
    // CARGAR ESTADO
    // ============================================================

    private async Task CargarEstado()
    {
        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(8);

            using HttpResponseMessage respuesta =
                await AppConfig.GetAsyncConToken(
                    cliente,
                    "/info");

            if (!respuesta.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "● Sin conexión";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            lblEstado.Text =
                "● Conectado";

            lblEstado.TextColor =
                Colors.LimeGreen;

            lblServidor.Text =
                AppConfig.Servidor;

            lblToken.Text =
                string.IsNullOrWhiteSpace(
                    AppConfig.Token)
                    ? "No configurado"
                    : "Configurado";
        }
        catch
        {
            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;

            lblServidor.Text =
                AppConfig.Servidor;

            lblToken.Text =
                "No disponible";
        }
    }


    // ============================================================
    // PROBAR CONEXIÓN
    // ============================================================

    private async void BtnProbar_Clicked(
        object sender,
        EventArgs e)
    {
        lblEstado.Text =
            "● Comprobando...";

        lblEstado.TextColor =
            Colors.Orange;

        await CargarEstado();
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
