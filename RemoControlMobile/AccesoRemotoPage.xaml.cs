using System.Net.Http.Json;

namespace RemoControlMobile;

public partial class AccesoRemotoPage : ContentPage
{
    public AccesoRemotoPage()
    {
        InitializeComponent();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarEstado();
    }


    private async Task CargarEstado()
    {
        try
        {
            lblEstado.Text =
                "Consultando...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(8);

            using HttpResponseMessage respuesta =
                await AppConfig.GetAsyncConToken(
                    cliente,
                    "/remote-access/status");

            if (!respuesta.IsSuccessStatusCode)
            {
                MostrarError();

                return;
            }

            RemoteAccessStatus? info =
                await respuesta.Content
                    .ReadFromJsonAsync<RemoteAccessStatus>();

            if (
                info == null ||
                !info.ok)
            {
                MostrarError();

                return;
            }

            if (info.authorized)
            {
                lblEstado.Text =
                    "● AUTORIZADO";

                lblEstado.TextColor =
                    Colors.LimeGreen;

                lblExpiracion.Text =
                    "Vence: " +
                    info.expiresAt;
            }
            else
            {
                lblEstado.Text =
                    "● NO AUTORIZADO";

                lblEstado.TextColor =
                    Colors.OrangeRed;

                lblExpiracion.Text =
                    info.pendingCode
                        ? "Código pendiente de validación"
                        : "";
            }
        }
        catch
        {
            MostrarError();
        }
    }


    private void MostrarError()
    {
        lblEstado.Text =
            "● Sin conexión";

        lblEstado.TextColor =
            Colors.Red;

        lblExpiracion.Text =
            "";
    }


    private async void BtnSolicitar_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(10);

            using HttpResponseMessage respuesta =
                await AppConfig.PostAsyncConToken(
                    cliente,
                    "/remote-access/request");

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Acceso remoto",
                    "No se pudo enviar el código.",
                    "Aceptar");

                return;
            }

            await DisplayAlertAsync(
                "Código enviado",
                "Revisa tu correo. El código vence en 5 minutos.",
                "Aceptar");

            await CargarEstado();
        }
        catch
        {
            await DisplayAlertAsync(
                "Acceso remoto",
                "Sin conexión con la PC.",
                "Aceptar");
        }
    }


    private async void BtnValidar_Clicked(
        object sender,
        EventArgs e)
    {
        string codigo =
            txtCodigo.Text ??
            "";

        if (codigo.Length != 6)
        {
            await DisplayAlertAsync(
                "Código",
                "Introduce los 6 dígitos.",
                "Aceptar");

            return;
        }

        int minutos =
            ObtenerMinutos();

        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(10);

            using HttpResponseMessage respuesta =
                await AppConfig.PostAsyncConToken(
                    cliente,
                    "/remote-access/verify?code=" +
                    Uri.EscapeDataString(
                        codigo) +
                    "&minutes=" +
                    minutos);

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Código",
                    "Código incorrecto, vencido o sin autorización.",
                    "Aceptar");

                return;
            }

            txtCodigo.Text =
                "";

            await DisplayAlertAsync(
                "RemoControl",
                "Autorización remota activada.",
                "Aceptar");

            await CargarEstado();
        }
        catch
        {
            await DisplayAlertAsync(
                "Acceso remoto",
                "Sin conexión con la PC.",
                "Aceptar");
        }
    }


    private int ObtenerMinutos()
    {
        switch (
            pickerDuracion.SelectedIndex)
        {
            case 0:
                return 30;

            case 2:
                return 240;

            case 3:
                return 480;

            default:
                return 60;
        }
    }


    private async void BtnRevocar_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar =
            await DisplayAlertAsync(
                "Revocar acceso",
                "¿Cancelar la autorización remota?",
                "Revocar",
                "Cancelar");

        if (!confirmar)
        {
            return;
        }

        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(8);

            await AppConfig.PostAsyncConToken(
                cliente,
                "/remote-access/revoke");

            await CargarEstado();
        }
        catch
        {
            MostrarError();
        }
    }


    private async void BtnActualizar_Clicked(
        object sender,
        EventArgs e)
    {
        await CargarEstado();
    }


    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}


public class RemoteAccessStatus
{
    public bool ok { get; set; }

    public bool authorized { get; set; }

    public bool pendingCode { get; set; }

    public string? expiresAt { get; set; }
}
