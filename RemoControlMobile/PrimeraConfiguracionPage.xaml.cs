using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RemoControlMobile;

public partial class PrimeraConfiguracionPage : ContentPage
{
    private bool tokenVisible;


    public PrimeraConfiguracionPage()
    {
        InitializeComponent();

        pickerVelocidad.SelectedIndex =
            1;

        txtServidor.Text =
            AppConfig.Servidor;

        txtToken.Text =
            AppConfig.Token;
    }


    // ============================================================
    // MOSTRAR TOKEN
    // ============================================================

    private void BtnMostrar_Clicked(
        object sender,
        EventArgs e)
    {
        tokenVisible =
            !tokenVisible;

        txtToken.IsPassword =
            !tokenVisible;

        btnMostrar.Text =
            tokenVisible
                ? "Ocultar"
                : "Ver";
    }


    // ============================================================
    // NORMALIZAR SERVIDOR
    // ============================================================

    private static string NormalizarServidor(
        string servidor)
    {
        return AppConfig.NormalizarServidor(
            servidor);
    }


    // ============================================================
    // PROBAR CONEXIÓN
    // ============================================================

    private async void BtnProbar_Clicked(
        object sender,
        EventArgs e)
    {
        string servidor =
            AppConfig.NormalizarServidor(
                txtServidor.Text);

        string token =
            AppConfig.LimpiarToken(
                txtToken.Text);


        if (string.IsNullOrWhiteSpace(
            servidor))
        {
            lblEstado.Text =
                "● Falta la dirección del servidor";

            lblEstado.TextColor =
                Colors.Red;

            return;
        }


        if (string.IsNullOrWhiteSpace(
            token))
        {
            lblEstado.Text =
                "● Falta el token";

            lblEstado.TextColor =
                Colors.Red;

            return;
        }


        if (!Uri.TryCreate(
            servidor,
            UriKind.Absolute,
            out Uri? uriServidor))
        {
            lblEstado.Text =
                "● Dirección no válida";

            lblEstado.TextColor =
                Colors.Red;

            return;
        }

        txtServidor.Text =
            servidor;

        txtToken.Text =
            token;


        try
        {
            btnProbar.IsEnabled =
                false;

            lblEstado.Text =
                "● Comprobando...";

            lblEstado.TextColor =
                Colors.Orange;


            using HttpClient cliente =
                new HttpClient();

            cliente.Timeout =
                TimeSpan.FromSeconds(
                    EsTailscale(uriServidor)
                        ? 25
                        : 8);

            AppConfig.AgregarTokenHeaders(
                cliente,
                token);


            HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    servidor +
                    "/status");

            if (
                respuesta.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized
                ||
                respuesta.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                respuesta.Dispose();

                respuesta =
                    await cliente.GetAsync(
                        AppConfig.AnexarTokenAUrl(
                            servidor +
                            "/status",
                            token));
            }

            using (respuesta)
            {
                if (respuesta.IsSuccessStatusCode)
                {
                    lblEstado.Text =
                        "● Conexión correcta";

                    lblEstado.TextColor =
                        Colors.LimeGreen;
                }
                else if (
                    respuesta.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized
                    ||
                    respuesta.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    lblEstado.Text =
                        "● Token incorrecto";

                    lblEstado.TextColor =
                        Colors.Red;
                }
                else
                {
                    lblEstado.Text =
                        "● El servidor respondió pero rechazó la solicitud";

                    lblEstado.TextColor =
                        Colors.Red;
                }
            }
        }
        catch (TaskCanceledException)
        {
            lblEstado.Text =
                EsTailscale(uriServidor)
                    ? "● Tailscale tardó demasiado"
                    : "● La PC no respondió a tiempo";

            lblEstado.TextColor =
                Colors.Red;
        }
        catch (HttpRequestException)
        {
            lblEstado.Text =
                EsTailscale(uriServidor)
                    ? "● La app no pudo abrir Tailscale"
                    : "● No se pudo alcanzar la PC";

            lblEstado.TextColor =
                Colors.Red;
        }
        catch
        {
            lblEstado.Text =
                "● No se pudo conectar";

            lblEstado.TextColor =
                Colors.Red;
        }
        finally
        {
            btnProbar.IsEnabled =
                true;
        }
    }

    private static bool EsTailscale(
        Uri uri)
    {
        return uri.Host.StartsWith(
            "100.",
            StringComparison.OrdinalIgnoreCase);
    }


    // ============================================================
    // FINALIZAR
    // ============================================================

    private async void BtnFinalizar_Clicked(
        object sender,
        EventArgs e)
    {
        string servidor =
            AppConfig.NormalizarServidor(
                txtServidor.Text);

        string token =
            AppConfig.LimpiarToken(
                txtToken.Text);


        if (string.IsNullOrWhiteSpace(
            servidor))
        {
            await DisplayAlertAsync(
                "Primera configuración",
                "Escribe la dirección que aparece en RemoControl Server.",
                "Aceptar");

            return;
        }


        if (!Uri.TryCreate(
            servidor,
            UriKind.Absolute,
            out _))
        {
            await DisplayAlertAsync(
                "Primera configuración",
                "La dirección del servidor no es válida.",
                "Aceptar");

            return;
        }

        txtServidor.Text =
            servidor;

        txtToken.Text =
            token;


        if (string.IsNullOrWhiteSpace(
            token))
        {
            await DisplayAlertAsync(
                "Primera configuración",
                "Escribe el token de seguridad.",
                "Aceptar");

            return;
        }


        AppConfig.Servidor =
            servidor;

        AppConfig.Token =
            token;


        switch (
            pickerVelocidad.SelectedIndex)
        {
            case 0:

                AppConfig.IntervaloPantalla =
                    180;

                break;

            case 2:

                AppConfig.IntervaloPantalla =
                    800;

                break;

            default:

                AppConfig.IntervaloPantalla =
                    350;

                break;
        }


        Preferences.Default.Set(
            "PrimeraConfiguracionCompletada",
            true);


        await DisplayAlertAsync(
            "MSI Center",
            "Configuración terminada. Ya puedes controlar tu PC.",
            "Comenzar");


        await Navigation.PopAsync();
    }
}
