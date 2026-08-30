using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RemoControlMobile;

public partial class PortapapelesPage : ContentPage
{
    public PortapapelesPage()
    {
        InitializeComponent();
    }

    private async void BtnLeerPc_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            lblEstado.Text =
                "Leyendo portapapeles de la PC...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(10);

            using HttpResponseMessage respuesta =
                await AppConfig.GetAsyncConToken(
                    cliente,
                    "/clipboard");

            if (!respuesta.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "No se pudo leer el portapapeles.";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            ClipboardRespuesta? datos =
                await respuesta.Content
                    .ReadFromJsonAsync
                    <ClipboardRespuesta>();

            if (
                datos == null ||
                !datos.ok)
            {
                lblEstado.Text =
                    datos?.error ??
                    "Portapapeles no disponible.";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            txtContenido.Text =
                datos.text ??
                "";

            lblEstado.Text =
                "● Portapapeles leído";

            lblEstado.TextColor =
                Colors.LimeGreen;
        }
        catch
        {
            lblEstado.Text =
                "Sin conexión con la PC.";

            lblEstado.TextColor =
                Colors.Red;
        }
    }

    private async void BtnEnviarPc_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            string texto =
                txtContenido.Text ??
                "";

            if (string.IsNullOrWhiteSpace(
                texto))
            {
                lblEstado.Text =
                    "Escribe un texto primero.";

                lblEstado.TextColor =
                    Colors.Orange;

                return;
            }

            lblEstado.Text =
                "Enviando a la PC...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(10);

            string json =
                JsonSerializer.Serialize(
                    new ClipboardEnviar
                    {
                        text = texto
                    });

            using StringContent contenido =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            using HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    AppConfig.UrlConToken(
                        "/clipboard"),
                    contenido);

            if (!respuesta.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "No se pudo enviar el texto.";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            lblEstado.Text =
                "● Texto enviado a la PC";

            lblEstado.TextColor =
                Colors.LimeGreen;
        }
        catch
        {
            lblEstado.Text =
                "Sin conexión con la PC.";

            lblEstado.TextColor =
                Colors.Red;
        }
    }
    private async void BtnPegarPc_Clicked(
    object sender,
    EventArgs e)
    {
        try
        {
            string texto =
                txtContenido.Text ??
                "";

            if (string.IsNullOrWhiteSpace(
                texto))
            {
                lblEstado.Text =
                    "Escribe un texto primero.";

                lblEstado.TextColor =
                    Colors.Orange;

                return;
            }

            lblEstado.Text =
                "Preparando texto...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(10);

            // Primero manda el texto al portapapeles.
            string json =
                System.Text.Json.JsonSerializer
                    .Serialize(
                        new ClipboardEnviar
                        {
                            text = texto
                        });

            using StringContent contenido =
                new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");

            using HttpResponseMessage copiar =
                await cliente.PostAsync(
                    AppConfig.UrlConToken(
                        "/clipboard"),
                    contenido);

            if (!copiar.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "No se pudo copiar el texto en la PC.";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            // Después ejecuta Ctrl+V en Windows.
            using HttpResponseMessage pegar =
                await AppConfig.PostAsyncConToken(
                    cliente,
                    "/clipboard/paste");

            if (!pegar.IsSuccessStatusCode)
            {
                lblEstado.Text =
                    "Se copió, pero no se pudo pegar.";

                lblEstado.TextColor =
                    Colors.Orange;

                return;
            }

            lblEstado.Text =
                "● Texto pegado en la PC";

            lblEstado.TextColor =
                Colors.LimeGreen;
        }
        catch
        {
            lblEstado.Text =
                "Sin conexión con la PC.";

            lblEstado.TextColor =
                Colors.Red;
        }
    }

    private async void BtnCopiarTelefono_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            string texto =
                txtContenido.Text ??
                "";

            if (string.IsNullOrWhiteSpace(
                texto))
            {
                lblEstado.Text =
                    "No hay texto para copiar.";

                lblEstado.TextColor =
                    Colors.Orange;

                return;
            }

            await Clipboard.Default
                .SetTextAsync(
                    texto);

            lblEstado.Text =
                "● Copiado al teléfono";

            lblEstado.TextColor =
                Colors.LimeGreen;
        }
        catch
        {
            lblEstado.Text =
                "No se pudo copiar.";

            lblEstado.TextColor =
                Colors.Red;
        }
    }

    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

public class ClipboardRespuesta
{
    public bool ok { get; set; }

    public string? text { get; set; }

    public string? error { get; set; }
}

public class ClipboardEnviar
{
    public string? text { get; set; }
}
