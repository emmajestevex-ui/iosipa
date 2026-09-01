using System.Globalization;
using System.Net.Http.Json;

namespace RemoControlMobile;

public partial class UbicacionPage : ContentPage
{
    private readonly HttpClient cliente =
        AppConfig.CrearCliente(10);

    public UbicacionPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarUbicacion();
    }

    private async Task CargarUbicacion()
    {
        try
        {
            lblEstado.Text =
                "Buscando ubicación...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    AppConfig.Servidor +
                    "/location");

            if (!respuesta.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "No se pudo obtener la ubicación";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            UbicacionPc? ubicacion =
                await respuesta.Content
                    .ReadFromJsonAsync<UbicacionPc>();

            if (ubicacion == null ||
                !ubicacion.ok)
            {
                lblEstado.Text =
                    "Ubicación no disponible";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            lblLatitud.Text =
                "Latitud: " +
                ubicacion.latitude.ToString(
                    CultureInfo.InvariantCulture);

            lblLongitud.Text =
                "Longitud: " +
                ubicacion.longitude.ToString(
                    CultureInfo.InvariantCulture);

            if (ubicacion.accuracy > 0)
            {
                lblPrecision.Text =
                    "Precisión aproximada: " +
                    Math.Round(
                        ubicacion.accuracy) +
                    " metros";
            }
            else
            {
                lblPrecision.Text =
                    "Precisión: no disponible";
            }

            if (!string.IsNullOrWhiteSpace(
                ubicacion.updatedAt))
            {
                lblUltimaActualizacion.Text =
                    "Última actualización: " +
                    ubicacion.updatedAt;
            }
            else
            {
                lblUltimaActualizacion.Text =
                    "Última actualización: --";
            }

            lblEstado.Text =
                "● Ubicación encontrada";

            lblEstado.TextColor =
                Colors.LimeGreen;

            CargarMapa(
                ubicacion.latitude,
                ubicacion.longitude);
        }
        catch (Exception ex)
        {
            lblEstado.Text =
                "Sin conexión con la PC";

            lblEstado.TextColor =
                Colors.Red;

            await DisplayAlertAsync(
                "Error",
                ex.Message,
                "Aceptar");
        }
    }

    private void CargarMapa(
        double latitud,
        double longitud)
    {
        string lat =
            latitud.ToString(
                CultureInfo.InvariantCulture);

        string lon =
            longitud.ToString(
                CultureInfo.InvariantCulture);

        double delta =
            0.01;

        string izquierda =
            (longitud - delta).ToString(
                CultureInfo.InvariantCulture);

        string derecha =
            (longitud + delta).ToString(
                CultureInfo.InvariantCulture);

        string abajo =
            (latitud - delta).ToString(
                CultureInfo.InvariantCulture);

        string arriba =
            (latitud + delta).ToString(
                CultureInfo.InvariantCulture);

        string url =
            "https://www.openstreetmap.org/export/embed.html" +
            "?bbox=" +
            Uri.EscapeDataString(
                izquierda + "," +
                abajo + "," +
                derecha + "," +
                arriba) +
            "&layer=mapnik" +
            "&marker=" +
            Uri.EscapeDataString(
                lat + "," + lon);

        mapaPc.Source =
            url;
    }

    private async void BtnActualizar_Clicked(
        object sender,
        EventArgs e)
    {
        await CargarUbicacion();
    }

    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

public class UbicacionPc
{
    public bool ok { get; set; }

    public double latitude { get; set; }

    public double longitude { get; set; }

    public double accuracy { get; set; }

    public string? updatedAt { get; set; }

    public string? message { get; set; }
}