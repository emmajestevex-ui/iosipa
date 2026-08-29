using System.Net.Http.Json;

namespace RemoControlMobile;

public partial class ActividadPage : ContentPage
{
    public ActividadPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarHistorial();
    }

    private async Task CargarHistorial()
    {
        try
        {
            cargando.IsVisible =
                true;

            cargando.IsRunning =
                true;

            lblEstado.Text =
                "Consultando actividad...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(10);

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    AppConfig.Servidor +
                    "/history");

            if (!respuesta.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "No se pudo obtener el historial";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            HistorialRespuesta? datos =
                await respuesta.Content
                    .ReadFromJsonAsync<HistorialRespuesta>();

            if (datos == null ||
                !datos.ok)
            {
                lblEstado.Text =
                    "Historial no disponible";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            lblHistorial.Text =
                string.IsNullOrWhiteSpace(
                    datos.history)
                    ? "No hay eventos registrados."
                    : datos.history;

            lblEstado.Text =
                "● Actualizado";

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
        finally
        {
            cargando.IsRunning =
                false;

            cargando.IsVisible =
                false;
        }
    }

    private async void BtnActualizar_Clicked(
        object sender,
        EventArgs e)
    {
        await CargarHistorial();
    }

    private void BtnLimpiar_Clicked(
        object sender,
        EventArgs e)
    {
        lblHistorial.Text =
            "Vista limpia. El historial de la PC no fue borrado.";

        lblEstado.Text =
            "Vista limpia";

        lblEstado.TextColor =
            Colors.Orange;
    }

    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

public class HistorialRespuesta
{
    public bool ok { get; set; }

    public string? history { get; set; }

    public string? error { get; set; }
}