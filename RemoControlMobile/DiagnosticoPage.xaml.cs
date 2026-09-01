using System.Diagnostics;

namespace RemoControlMobile;

public partial class DiagnosticoPage : ContentPage
{
    public DiagnosticoPage()
    {
        InitializeComponent();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await EjecutarDiagnostico();
    }


    private async Task EjecutarDiagnostico()
    {
        try
        {
            lblServidor.Text =
                AppConfig.Servidor;

            lblTipo.Text =
                "Conexión: " +
                ObtenerTipoConexion();

            lblEstado.Text =
                "Probando...";

            lblEstado.TextColor =
                Colors.Orange;

            lblResultado.Text =
                "Comprobando respuesta del servidor...";

            Stopwatch reloj =
                Stopwatch.StartNew();

            using HttpClient cliente =
                AppConfig.CrearCliente(10);

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    AppConfig.Servidor +
                    "/status");

            reloj.Stop();

            long milisegundos =
                reloj.ElapsedMilliseconds;

            lblLatencia.Text =
                milisegundos +
                " ms";

            if (!respuesta.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "Error";

                lblEstado.TextColor =
                    Colors.Red;

                lblResultado.Text =
                    "El servidor respondió con código " +
                    (int)respuesta.StatusCode +
                    ".";

                return;
            }

            lblEstado.Text =
                "En línea";

            lblEstado.TextColor =
                Colors.LimeGreen;

            string calidad;

            if (milisegundos <= 50)
            {
                calidad =
                    "Excelente";
            }
            else if (milisegundos <= 120)
            {
                calidad =
                    "Buena";
            }
            else if (milisegundos <= 250)
            {
                calidad =
                    "Regular";
            }
            else
            {
                calidad =
                    "Lenta";
            }

            lblResultado.Text =
                "Servidor accesible.\n" +
                "Tipo: " +
                ObtenerTipoConexion() +
                "\n" +
                "Latencia: " +
                milisegundos +
                " ms\n" +
                "Calidad: " +
                calidad;
        }
        catch (TaskCanceledException)
        {
            lblEstado.Text =
                "Sin respuesta";

            lblEstado.TextColor =
                Colors.Red;

            lblLatencia.Text =
                "-- ms";

            lblResultado.Text =
                "La PC tardó demasiado en responder.";
        }
        catch
        {
            lblEstado.Text =
                "Sin conexión";

            lblEstado.TextColor =
                Colors.Red;

            lblLatencia.Text =
                "-- ms";

            lblResultado.Text =
                "No fue posible conectar con RemoControl PC.";
        }
    }


    private string ObtenerTipoConexion()
    {
        string servidor =
            AppConfig.Servidor ??
            "";

        if (
            servidor.Contains(
                "100."))
        {
            return "Tailscale";
        }

        if (
            servidor.Contains(
                "192.168.") ||
            servidor.Contains(
                "10.") ||
            servidor.Contains(
                "172."))
        {
            return "Red local";
        }

        return
            "Servidor personalizado";
    }


    private async void BtnDiagnosticar_Clicked(
        object sender,
        EventArgs e)
    {
        await EjecutarDiagnostico();
    }


    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}