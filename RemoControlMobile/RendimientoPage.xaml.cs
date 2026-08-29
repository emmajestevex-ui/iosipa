using System.Net.Http.Json;

namespace RemoControlMobile;

public partial class RendimientoPage : ContentPage
{
    public RendimientoPage()
    {
        InitializeComponent();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await ConsultarModo();
    }


    private async Task ConsultarModo()
    {
        try
        {
            lblModo.Text =
                "Consultando...";

            lblModo.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(10);

            RendimientoRespuesta? info =
                await cliente.GetFromJsonAsync
                <RendimientoRespuesta>(
                    AppConfig.Servidor +
                    "/performance");

            if (
                info == null ||
                !info.ok)
            {
                MostrarError();

                return;
            }

            lblModo.Text =
                ObtenerNombre(
                    info.mode);

            lblModo.TextColor =
                ObtenerColor(
                    info.mode);

            lblDetalle.Text =
                info.plan ?? "";
        }
        catch
        {
            MostrarError();
        }
    }


    private async Task CambiarModo(
        string modo)
    {
        try
        {
            lblModo.Text =
                "Aplicando...";

            lblModo.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(12);

            using HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    AppConfig.Servidor +
                    "/performance?mode=" +
                    Uri.EscapeDataString(
                        modo),
                    null);

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Rendimiento",
                    "No se pudo cambiar el modo.",
                    "Aceptar");

                await ConsultarModo();

                return;
            }

            await ConsultarModo();
        }
        catch
        {
            MostrarError();
        }
    }


    private static string ObtenerNombre(
        string? modo)
    {
        switch (
            modo?.ToLowerInvariant())
        {
            case "powersaver":
                return "🌿 Ahorro";

            case "balanced":
                return "⚖️ Equilibrado";

            case "high":
                return "🚀 Alto rendimiento";

            case "turbo":
                return "🔥 TURBO";

            default:
                return "Modo personalizado";
        }
    }


    private static Color ObtenerColor(
        string? modo)
    {
        switch (
            modo?.ToLowerInvariant())
        {
            case "powersaver":
                return Colors.LimeGreen;

            case "balanced":
                return Colors.DeepSkyBlue;

            case "high":
                return Colors.Orange;

            case "turbo":
                return Colors.OrangeRed;

            default:
                return Colors.White;
        }
    }


    private void MostrarError()
    {
        lblModo.Text =
            "Sin conexión";

        lblModo.TextColor =
            Colors.Red;

        lblDetalle.Text =
            "No se pudo consultar el modo de rendimiento.";
    }


    private async void BtnAhorro_Clicked(
        object sender,
        EventArgs e)
    {
        await CambiarModo(
            "powersaver");
    }


    private async void BtnEquilibrado_Clicked(
        object sender,
        EventArgs e)
    {
        await CambiarModo(
            "balanced");
    }


    private async void BtnAlto_Clicked(
        object sender,
        EventArgs e)
    {
        await CambiarModo(
            "high");
    }


    private async void BtnTurbo_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar =
            await DisplayAlertAsync(
                "Modo Turbo",
                "¿Activar máximo rendimiento de Windows?",
                "Activar Turbo",
                "Cancelar");

        if (confirmar)
        {
            await CambiarModo(
                "turbo");
        }
    }


    private async void BtnActualizar_Clicked(
        object sender,
        EventArgs e)
    {
        await ConsultarModo();
    }


    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}


public class RendimientoRespuesta
{
    public bool ok { get; set; }

    public string? mode { get; set; }

    public string? plan { get; set; }

    public string? error { get; set; }
}