using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RemoControlMobile;

public partial class ConfiguracionPage : ContentPage
{
    private bool tokenVisible = false;


    public ConfiguracionPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);
        txtNombreApp.Text = AppConfig.NombrePersonalizado;
        swBloqueo.IsToggled = AppConfig.BloqueoApp;
        lblLogo.Text = string.IsNullOrWhiteSpace(AppConfig.LogoPersonalizado) ? "Logo predeterminado" : Path.GetFileName(AppConfig.LogoPersonalizado);
        txtColorFondo.Text = AppConfig.ColorFondo;
        pickerHablar.SelectedIndex = AppConfig.DuracionHablarSegundos switch { 2 => 0, 4 => 1, 6 => 2, 10 => 3, _ => 4 };
        pickerAudioLive.SelectedIndex = Math.Clamp(AppConfig.AudioLiveSegundos - 1, 0, 2);

        CargarConfiguracion();

        lblVersion.Text =
            "Versión " +
            AppInfo.Current.VersionString;
    }

    private async void BtnAyudaTailscale_Clicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlertAsync(
            "Tailscale",
            "Usa la DIRECCIÓN TAILSCALE de la PC, no la IP del iPhone.\n\n" +
            "En Tailscale ambos deben aparecer conectados.\n\n" +
            "Prueba rápida: abre Safari en el iPhone y entra a la dirección Tailscale de la PC terminando en /status.\n\n" +
            "Si Safari muestra 401 o Unauthorized, Tailscale sí llega a la PC y debes revisar el token o instalar el IPA nuevo.\n\n" +
            "Si Safari no abre la página, ejecuta REPARAR_TODO_REMOCONTROL_ADMIN.bat como administrador y revisa que Tailscale siga conectado.",
            "Aceptar");
    }


    // ============================================================
    // CARGAR CONFIGURACIÓN
    // ============================================================

    private void CargarConfiguracion()
    {
        txtServidor.Text =
            AppConfig.Servidor;

        txtToken.Text =
            AppConfig.Token;


        int intervalo =
            AppConfig.IntervaloPantalla;


        if (intervalo <= 220)
        {
            pickerVelocidad.SelectedIndex = 0;
        }
        else if (intervalo <= 500)
        {
            pickerVelocidad.SelectedIndex = 1;
        }
        else
        {
            pickerVelocidad.SelectedIndex = 2;
        }


        lblPrueba.Text =
            "● Sin comprobar";

        lblPrueba.TextColor =
            Colors.Orange;


        ActualizarTipoConexion();
    }


    // ============================================================
    // DETECTAR TIPO DE CONEXIÓN
    // ============================================================

    private void ActualizarTipoConexion()
    {
        string servidor =
            AppConfig.Servidor?
                .Trim()
            ??
            "";


        lblServidorActivo.Text =
            string.IsNullOrWhiteSpace(
                servidor)
                ? "No configurado"
                : servidor;


        if (string.IsNullOrWhiteSpace(
            servidor))
        {
            lblTipoConexion.Text =
                "● Sin configurar";

            lblTipoConexion.TextColor =
                Colors.Orange;

            return;
        }


        Uri? uri;


        if (!Uri.TryCreate(
            servidor,
            UriKind.Absolute,
            out uri))
        {
            lblTipoConexion.Text =
                "● Dirección personalizada";

            lblTipoConexion.TextColor =
                Colors.Orange;

            return;
        }


        string host =
            uri.Host;


        if (host.StartsWith(
            "100.",
            StringComparison.OrdinalIgnoreCase))
        {
            lblTipoConexion.Text =
                "● Tailscale";

            lblTipoConexion.TextColor =
                Colors.DeepSkyBlue;

            return;
        }


        if (
            host.StartsWith("192.168.") ||
            host.StartsWith("10.") ||
            host.StartsWith("172.16.") ||
            host.StartsWith("172.17.") ||
            host.StartsWith("172.18.") ||
            host.StartsWith("172.19.") ||
            host.StartsWith("172.20.") ||
            host.StartsWith("172.21.") ||
            host.StartsWith("172.22.") ||
            host.StartsWith("172.23.") ||
            host.StartsWith("172.24.") ||
            host.StartsWith("172.25.") ||
            host.StartsWith("172.26.") ||
            host.StartsWith("172.27.") ||
            host.StartsWith("172.28.") ||
            host.StartsWith("172.29.") ||
            host.StartsWith("172.30.") ||
            host.StartsWith("172.31."))
        {
            lblTipoConexion.Text =
                "● Wi-Fi / red local";

            lblTipoConexion.TextColor =
                Colors.LimeGreen;

            return;
        }


        lblTipoConexion.Text =
            "● Servidor personalizado";

        lblTipoConexion.TextColor =
            Colors.Orange;
    }


    // ============================================================
    // MOSTRAR / OCULTAR TOKEN
    // ============================================================

    private void BtnMostrarToken_Clicked(
        object sender,
        EventArgs e)
    {
        tokenVisible =
            !tokenVisible;


        txtToken.IsPassword =
            !tokenVisible;


        btnMostrarToken.Text =
            tokenVisible
                ? "Ocultar"
                : "Ver";
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
            lblPrueba.Text =
                "● Escribe la dirección de la PC";

            lblPrueba.TextColor =
                Colors.Red;

            return;
        }


        if (!Uri.TryCreate(
            servidor,
            UriKind.Absolute,
            out Uri? uriServidor))
        {
            lblPrueba.Text =
                "● La dirección no es válida";

            lblPrueba.TextColor =
                Colors.Red;

            return;
        }

        txtServidor.Text =
            servidor;

        txtToken.Text =
            token;


        if (string.IsNullOrWhiteSpace(
            token))
        {
            lblPrueba.Text =
                "● Escribe el token de seguridad";

            lblPrueba.TextColor =
                Colors.Red;

            return;
        }


        try
        {
            btnProbar.IsEnabled =
                false;


            lblPrueba.Text =
                "● Buscando la PC...";

            lblPrueba.TextColor =
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
                    lblPrueba.Text =
                        "● Conexión correcta";

                    lblPrueba.TextColor =
                        Colors.LimeGreen;
                }
                else if (
                    respuesta.StatusCode ==
                    System.Net.HttpStatusCode.Unauthorized
                    ||
                    respuesta.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    lblPrueba.Text =
                        "● Token incorrecto";

                    lblPrueba.TextColor =
                        Colors.Red;
                }
                else
                {
                    lblPrueba.Text =
                        "● La PC respondió, pero rechazó la conexión";

                    lblPrueba.TextColor =
                        Colors.Red;
                }
            }
        }
        catch (TaskCanceledException)
        {
            lblPrueba.Text =
                EsTailscale(uriServidor)
                    ? "● Tailscale tardó demasiado"
                    : "● Tiempo agotado. Revisa la IP y la conexión.";

            lblPrueba.TextColor =
                Colors.Red;

            await MostrarAyudaConexionFallida(
                servidor,
                "Tiempo agotado.");
        }
        catch (HttpRequestException ex)
        {
            lblPrueba.Text =
                EsTailscale(uriServidor)
                    ? "● La app no pudo abrir Tailscale"
                    : "● No se encontró la PC";

            lblPrueba.TextColor =
                Colors.Red;

            await MostrarAyudaConexionFallida(
                servidor,
                ex.Message);
        }
        catch (Exception ex)
        {
            lblPrueba.Text =
                "● No se pudo conectar";

            lblPrueba.TextColor =
                Colors.Red;

            await MostrarAyudaConexionFallida(
                servidor,
                ex.Message);
        }
        finally
        {
            btnProbar.IsEnabled =
                true;
        }
    }


    // ============================================================
    // GUARDAR
    // ============================================================

    private async void BtnGuardar_Clicked(
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
                "Configuración",
                "Debes escribir la DIRECCIÓN PARA EL TELÉFONO que muestra RemoControl en la PC.",
                "Aceptar");

            return;
        }


        if (!Uri.TryCreate(
            servidor,
            UriKind.Absolute,
            out _))
        {
            await DisplayAlertAsync(
                "Configuración",
                "La dirección de la PC no es válida.",
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
                "Configuración",
                "Debes escribir el TOKEN DE SEGURIDAD que muestra RemoControl en la PC.",
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


        ActualizarTipoConexion();


        await DisplayAlertAsync(
            "MSI Center",
            "Configuración guardada correctamente.\n\nYa puedes controlar esta PC desde el teléfono.",
            "Aceptar");


        await Navigation.PopAsync();
    }

    private async Task MostrarAyudaConexionFallida(
        string servidor,
        string detalle)
    {
        if (!Uri.TryCreate(
                servidor,
                UriKind.Absolute,
                out Uri? uri))
        {
            return;
        }

        if (!EsTailscale(uri))
            return;

        await DisplayAlertAsync(
            "Tailscale",
            "No se pudo llegar a la PC por Tailscale.\n\n" +
            "Revisa esto:\n" +
            "1. La dirección debe ser la de la PC, no la del iPhone.\n" +
            "2. Tailscale debe estar conectado en ambos dispositivos.\n" +
            "3. RemoControl debe estar abierto en Windows.\n" +
            "4. En Safari del iPhone prueba esta dirección: " + servidor.TrimEnd('/') + "/status\n" +
            "5. Si Safari muestra 401 o Unauthorized, la conexión funciona y debes revisar el token o instalar el IPA nuevo.\n" +
            "6. Si Safari no abre, ejecuta REPARAR_TODO_REMOCONTROL_ADMIN.bat como administrador.\n\n" +
            "Detalle: " + Recortar(detalle, 220),
            "Aceptar");
    }

    private static bool EsTailscale(
        Uri uri)
    {
        return uri.Host.StartsWith(
            "100.",
            StringComparison.OrdinalIgnoreCase);
    }


    // ============================================================
    // RESTAURAR
    // ============================================================

    private async void BtnRestaurar_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar =
            await DisplayAlertAsync(
                "Restaurar configuración",
                "¿Quieres borrar la dirección y el token guardados?",
                "Restaurar",
                "Cancelar");


        if (!confirmar)
        {
            return;
        }


        AppConfig.Restaurar();


        Preferences.Default.Set(
            "PrimeraConfiguracionCompletada",
            false);


        tokenVisible =
            false;


        txtToken.IsPassword =
            true;


        btnMostrarToken.Text =
            "Ver";


        CargarConfiguracion();


        lblPrueba.Text =
            "● Configuración restaurada";

        lblPrueba.TextColor =
            Colors.Orange;
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
    private async void BtnElegirLogo_Clicked(object sender, EventArgs e)
    {
        try
        {
            FileResult? archivo = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Selecciona tu logo",
                    FileTypes = FilePickerFileType.Images
                });

            if (archivo == null)
            {
                return;
            }

            string extension = Path.GetExtension(archivo.FileName);
            string destino = Path.Combine(
                FileSystem.AppDataDirectory,
                "brand_logo" + (string.IsNullOrWhiteSpace(extension) ? ".png" : extension));

            await using Stream origen = await archivo.OpenReadAsync();
            await using FileStream salida = File.Create(destino);
            await origen.CopyToAsync(salida);

            AppConfig.LogoPersonalizado = destino;
            lblLogo.Text = archivo.FileName;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Logo",
                "No se pudo guardar el logo.\n\n" + ex.Message,
                "Aceptar");
        }
    }
    private async void BtnGuardarPersonalizacion_Clicked(object sender, EventArgs e)
    {
        AppConfig.NombrePersonalizado = txtNombreApp.Text ?? "MSI Center";
        AppConfig.BloqueoApp = swBloqueo.IsToggled;
        AppConfig.ColorFondo = string.IsNullOrWhiteSpace(txtColorFondo.Text) ? "#0B1119" : txtColorFondo.Text.Trim();
        int[] hablar = { 2, 4, 6, 10, 15 };
        AppConfig.DuracionHablarSegundos = hablar[Math.Clamp(pickerHablar.SelectedIndex, 0, hablar.Length - 1)];
        AppConfig.AudioLiveSegundos = Math.Clamp(pickerAudioLive.SelectedIndex + 1, 1, 3);
        await DisplayAlertAsync("Guardado","Personalización y tiempos del intercomunicador guardados.","Aceptar");
    }

    private async void BtnCrearAcceso_Clicked(object sender, EventArgs e)
    {
        try
        {
            bool ok = await LauncherBranding.CrearAccesoPersonalizadoAsync(
                AppConfig.NombrePersonalizado,
                AppConfig.LogoPersonalizado,
                AppConfig.ColorFondo);

            await DisplayAlertAsync(
                "Acceso personalizado",
                ok ? "Se solicitó crear el acceso personalizado. Confirma la ventana de tu launcher si aparece." : "Este dispositivo o launcher no permite crear accesos personalizados automáticamente.",
                "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Acceso personalizado", ex.Message, "Aceptar");
        }
    }

    private static string Recortar(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Sin detalle.";

        text = text.Trim();
        return text.Length <= max ? text : text[..max];
    }
}
