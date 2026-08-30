using System.Net;
using System.Net.Http.Json;

namespace RemoControlMobile;

public partial class MainPage : ContentPage
{
    private bool comprobandoPrimeraConfiguracion;
    private bool comprobandoConexion;


    public MainPage()
    {
        InitializeComponent();

        Appearing +=
            MainPage_Appearing;
    }


    // ============================================================
    // APARECER
    // ============================================================

    private async void MainPage_Appearing(
        object? sender,
        EventArgs e)
    {
        await NotificationService
            .PedirPermiso();

        ActualizarServidorMostrado();

        await ComprobarConexion();

        await ComprobarActualizacion();
    }


    // ============================================================
    // PRIMERA CONFIGURACIÓN
    // ============================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AplicarPersonalizacion();

        if (comprobandoPrimeraConfiguracion)
        {
            return;
        }


        bool completada =
            Preferences.Default.Get(
                "PrimeraConfiguracionCompletada",
                false);


        if (!completada)
        {
            comprobandoPrimeraConfiguracion =
                true;

            await Navigation.PushAsync(
                new PrimeraConfiguracionPage());

            comprobandoPrimeraConfiguracion =
                false;
        }
    }


    // ============================================================
    // PERSONALIZACIÓN LOCAL
    // ============================================================

    private void AplicarPersonalizacion()
    {
        lblBrandName.Text = AppConfig.NombrePersonalizado;

        string logo = AppConfig.LogoPersonalizado;

        if (!string.IsNullOrWhiteSpace(logo) && File.Exists(logo))
        {
            imgBrandLogo.Source = ImageSource.FromFile(logo);
        }
        else
        {
            imgBrandLogo.Source = "default_brand.svg";
        }

        try
        {
            paginaPrincipal.BackgroundColor = Color.FromArgb(AppConfig.ColorFondo);
        }
        catch
        {
            paginaPrincipal.BackgroundColor = Color.FromArgb("#0B1119");
        }
    }


    // ============================================================
    // SERVIDOR MOSTRADO
    // ============================================================

    private void ActualizarServidorMostrado()
    {
        string servidor =
            AppConfig.Servidor ?? "";


        if (string.IsNullOrWhiteSpace(
            servidor))
        {
            lblServidor.Text =
                "Servidor: no configurado";

            return;
        }


        lblServidor.Text =
            "Servidor: " +
            servidor;
    }


    // ============================================================
    // CONEXIÓN
    // ============================================================

    private async Task ComprobarConexion()
    {
        if (comprobandoConexion)
        {
            return;
        }

        comprobandoConexion = true;

        try
        {
            ActualizarServidorMostrado();

            if (!AppConfig.HayConfiguracion)
            {
                lblPc.Text = "Sin configurar";
                lblEstado.Text = "● Configura tu PC";
                lblEstado.TextColor = Colors.Orange;
                lblUsuario.Text =
                    "Abre Configuración y copia la dirección y el token que muestra RemoControl en tu propia PC.";
                lblWindows.Text = "";
                return;
            }

            lblEstado.Text = "● Conectando...";
            lblEstado.TextColor = Colors.Orange;
            lblPc.Text = "Buscando tu PC...";
            lblUsuario.Text = "";
            lblWindows.Text = "";

            using HttpClient cliente =
                AppConfig.CrearCliente(8);

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    AppConfig.Servidor.TrimEnd('/') +
                    "/info");

            if (
                respuesta.StatusCode == HttpStatusCode.Unauthorized ||
                respuesta.StatusCode == HttpStatusCode.Forbidden)
            {
                lblPc.Text = "PC encontrada";
                lblEstado.Text = "● Token incorrecto";
                lblEstado.TextColor = Colors.Red;
                lblUsuario.Text =
                    "La PC respondió, pero el token no coincide. Copia de nuevo el TOKEN DE SEGURIDAD que muestra esa PC.";
                lblWindows.Text = "";
                return;
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                lblPc.Text = "PC encontrada";
                lblEstado.Text = "● Error del servidor";
                lblEstado.TextColor = Colors.Red;
                lblUsuario.Text =
                    "RemoControl en la PC respondió con un error. Revisa Configuración en la PC.";
                lblWindows.Text = "";
                return;
            }

            InfoPc? info =
                await respuesta.Content
                    .ReadFromJsonAsync<InfoPc>();

            if (info == null)
            {
                lblPc.Text = "PC encontrada";
                lblEstado.Text = "● Respuesta inválida";
                lblEstado.TextColor = Colors.Red;
                lblUsuario.Text =
                    "La respuesta del servidor no es válida.";
                lblWindows.Text = "";
                return;
            }

            lblPc.Text =
                info.pc ?? "PC";

            lblUsuario.Text =
                string.IsNullOrWhiteSpace(info.user)
                    ? ""
                    : "Usuario: " + info.user;

            lblWindows.Text =
                info.windows ?? "";

            lblEstado.Text = "● En línea";
            lblEstado.TextColor = Colors.LimeGreen;
        }
        catch (TaskCanceledException)
        {
            lblPc.Text = "PC sin respuesta";
            lblEstado.Text = "● Tiempo agotado";
            lblEstado.TextColor = Colors.Red;
            lblUsuario.Text =
                "Revisa que la dirección sea la de TU PC, que RemoControl esté abierto y que ambos estén en la misma red.";
            lblWindows.Text = "";
        }
        catch (HttpRequestException)
        {
            lblPc.Text = "PC no encontrada";
            lblEstado.Text = "● Sin conexión";
            lblEstado.TextColor = Colors.Red;
            lblUsuario.Text =
                "No se pudo alcanzar esa dirección. Revisa la IP, el puerto 5050 y el permiso de Windows en RemoControl PC.";
            lblWindows.Text = "";
        }
        catch (Exception ex)
        {
            lblPc.Text = "PC no encontrada";
            lblEstado.Text = "● Sin conexión";
            lblEstado.TextColor = Colors.Red;
            lblUsuario.Text = ex.Message;
            lblWindows.Text = "";
        }
        finally
        {
            comprobandoConexion = false;
        }
    }


    // ============================================================
    // ACTUALIZAR CONEXIÓN
    // ============================================================

    private async void BtnActualizar_Clicked(
        object sender,
        EventArgs e)
    {
        await ComprobarConexion();
    }


    // ============================================================
    // CONFIGURAR CONEXIÓN
    // ============================================================

    private async void BtnConfigurarConexion_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ConfiguracionPage());
    }


    // ============================================================
    // AYUDA
    // ============================================================

    private async void BtnAyuda_Clicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlertAsync(
            "Cómo conectar RemoControl",
            "1. Conecta el teléfono y la computadora al mismo Wi-Fi.\n\n" +
            "2. Abre RemoControl en la PC y entra a Configuración.\n\n" +
            "3. En la PC busca DIRECCIÓN PARA EL TELÉFONO y TOKEN DE SEGURIDAD.\n\n" +
            "4. Si la PC muestra un problema de acceso de Windows, pulsa Reparar acceso de Windows y acepta el permiso una sola vez.\n\n" +
            "5. En el teléfono abre Configuración y copia exactamente esos dos datos.\n\n" +
            "6. Pulsa Probar conexión y después Guardar cambios.\n\n" +
            "7. Si vas a usar RemoControl desde otra red, configura primero la conexión local y luego usa Tailscale en ambos dispositivos.\n\n" +
            "Telegram se configura desde RemoControl PC > Configuración. La app de PC explica paso a paso cómo crear el bot con BotFather y probar una notificación.",
            "Entendido");
    }


    // ============================================================
    // PANTALLA
    // ============================================================

    private async void BtnPantalla_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new PantallaPage());
    }


    // ============================================================
    // TOUCHPAD
    // ============================================================

    private async void BtnTouchpad_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new TouchpadPage());
    }


    // ============================================================
    // TECLADO
    // ============================================================

    private async void BtnTeclado_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new TecladoPage());
    }


    // ============================================================
    // EQUIPOS
    // ============================================================

    private async void BtnEquipos_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new EquiposPage());
    }


    // ============================================================
    // FAVORITOS
    // ============================================================

    private async void BtnFavoritos_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new FavoritosPage());
    }


    // ============================================================
    // RENDIMIENTO
    // ============================================================

    private async void BtnRendimiento_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new RendimientoPage());
    }


    // ============================================================
    // APLICACIONES
    // ============================================================

    private async void BtnAplicaciones_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new AplicacionesPage());
    }


    // ============================================================
    // ARCHIVOS
    // ============================================================

    private async void BtnArchivos_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ArchivosPage());
    }


    // ============================================================
    // PORTAPAPELES
    // ============================================================

    private async void BtnPortapapeles_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new PortapapelesPage());
    }


    // ============================================================
    // MULTIMEDIA
    // ============================================================

    private async void BtnMultimedia_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new MultimediaPage());
    }


    // ============================================================
    // ACCIONES
    // ============================================================

    private async void BtnAcciones_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new AccionesPage());
    }


    // ============================================================
    // ESTADO PC
    // ============================================================

    private async void BtnEstadoPc_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new EstadoPcPage());
    }


    // ============================================================
    // UBICACIÓN
    // ============================================================

    private async void BtnUbicacion_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new UbicacionPage());
    }


    // ============================================================
    // ACTIVIDAD
    // ============================================================

    private async void BtnActividad_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ActividadPage());
    }


    // ============================================================
    // ACCESO REMOTO
    // ============================================================

    private async void BtnAccesoRemoto_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new AccesoRemotoPage());
    }


    // ============================================================
    // SEGURIDAD
    // ============================================================

    private async void BtnSeguridad_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new SeguridadPage());
    }


    // ============================================================
    // DIAGNÓSTICO
    // ============================================================

    private async void BtnDiagnostico_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new DiagnosticoPage());
    }


    // ============================================================
    // ACTUALIZACIONES
    // ============================================================

    private async void BtnActualizaciones_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ActualizacionesPage());
    }


    // ============================================================
    // CONFIGURACIÓN
    // ============================================================

    private async void BtnConfiguracion_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ConfiguracionPage());
    }


    // ============================================================
    // COMPROBAR ACTUALIZACIÓN
    // ============================================================

    private async Task ComprobarActualizacion()
    {
        try
        {
            UpdateInfo? info =
                await UpdateService
                    .BuscarActualizacion();


            if (info == null)
            {
                return;
            }


            if (!UpdateService
                .HayActualizacion(
                    info))
            {
                return;
            }


            string mensaje =
                "Versión disponible: " +
                (
                    info.versionName ??
                    "Nueva versión"
                );


            if (!string.IsNullOrWhiteSpace(
                info.notes))
            {
                mensaje +=
                    "\n\n" +
                    info.notes;
            }


            bool actualizar =
                await DisplayAlertAsync(
                    "Nueva actualización",
                    mensaje,
                    "Actualizar ahora",
                    "Después");


            if (!actualizar)
            {
                return;
            }


            if (string.IsNullOrWhiteSpace(
                info.apkUrl))
            {
                await DisplayAlertAsync(
                    "Actualización",
                    "No se encontró el enlace del APK.",
                    "Aceptar");

                return;
            }


            await Launcher.Default.OpenAsync(
                info.apkUrl);
        }
        catch
        {
            // No interrumpir el inicio
            // si falla la comprobación.
        }
    }


    // ============================================================
    // INTERCOMUNICADOR
    // ============================================================

    private async void BtnIntercom_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new IntercomPage());
    }

    private async void BtnHerramientas_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HerramientasProPage());
    }

}


// ============================================================
// INFORMACIÓN DE LA PC
// ============================================================

public class InfoPc
{
    public bool ok
    {
        get;
        set;
    }


    public string? pc
    {
        get;
        set;
    }


    public string? user
    {
        get;
        set;
    }


    public string? windows
    {
        get;
        set;
    }


    public bool bit64
    {
        get;
        set;
    }


    public int screenWidth
    {
        get;
        set;
    }


    public int screenHeight
    {
        get;
        set;
    }
}