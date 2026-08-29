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


        comprobandoConexion =
            true;


        try
        {
            lblEstado.Text =
                "● Buscando PC...";

            lblEstado.TextColor =
                Colors.Orange;

            lblPc.Text =
                "Buscando...";

            lblUsuario.Text =
                "";

            lblWindows.Text =
                "";


            bool encontrado =
                await AppConfig
                    .DetectarServidor();


            ActualizarServidorMostrado();


            if (!encontrado)
            {
                lblPc.Text =
                    "PC no encontrada";

                lblEstado.Text =
                    "● Sin conexión";

                lblEstado.TextColor =
                    Colors.Red;

                lblUsuario.Text =
                    "No se encontró la PC por Wi-Fi ni Tailscale.";

                lblWindows.Text =
                    "";

                return;
            }


            using HttpClient cliente =
                AppConfig.CrearCliente(
                    8);


            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    AppConfig.Servidor +
                    "/info");


            if (
                respuesta.StatusCode ==
                HttpStatusCode.Unauthorized
                ||
                respuesta.StatusCode ==
                HttpStatusCode.Forbidden)
            {
                lblPc.Text =
                    "PC encontrada";

                lblEstado.Text =
                    "● Token incorrecto";

                lblEstado.TextColor =
                    Colors.Red;

                lblUsuario.Text =
                    "La PC respondió, pero rechazó el token.";

                lblWindows.Text =
                    "";

                return;
            }


            if (!respuesta.IsSuccessStatusCode)
            {
                lblPc.Text =
                    "PC encontrada";

                lblEstado.Text =
                    "● Error del servidor";

                lblEstado.TextColor =
                    Colors.Red;

                lblUsuario.Text =
                    "RemoControl Server respondió con un error.";

                lblWindows.Text =
                    "";

                return;
            }


            InfoPc? info =
                await respuesta.Content
                    .ReadFromJsonAsync
                    <InfoPc>();


            if (info == null)
            {
                lblPc.Text =
                    "PC encontrada";

                lblEstado.Text =
                    "● Respuesta inválida";

                lblEstado.TextColor =
                    Colors.Red;

                lblUsuario.Text =
                    "La respuesta del servidor no es válida.";

                lblWindows.Text =
                    "";

                return;
            }


            lblPc.Text =
                info.pc ??
                "PC";


            lblUsuario.Text =
                string.IsNullOrWhiteSpace(
                    info.user)
                    ? ""
                    : "Usuario: " +
                      info.user;


            lblWindows.Text =
                info.windows ??
                "";


            lblEstado.Text =
                "● En línea";

            lblEstado.TextColor =
                Colors.LimeGreen;
        }
        catch (TaskCanceledException)
        {
            lblPc.Text =
                "PC sin respuesta";

            lblEstado.Text =
                "● Tiempo agotado";

            lblEstado.TextColor =
                Colors.Red;

            lblUsuario.Text =
                "Comprueba RemoControl Server y Tailscale.";

            lblWindows.Text =
                "";
        }
        catch (HttpRequestException)
        {
            lblPc.Text =
                "PC no encontrada";

            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;

            lblUsuario.Text =
                "No se pudo alcanzar RemoControl Server.";

            lblWindows.Text =
                "";
        }
        catch
        {
            lblPc.Text =
                "PC no encontrada";

            lblUsuario.Text =
                "No se pudo conectar con RemoControl.";

            lblWindows.Text =
                "";

            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;
        }
        finally
        {
            comprobandoConexion =
                false;
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
            "1. Enciende la computadora.\n\n" +

            "2. Abre RemoControl Server en la PC.\n\n" +

            "3. Pulsa Iniciar servidor.\n\n" +

            "4. Si el teléfono y la PC están en la misma Wi-Fi, usa la dirección local que muestra el servidor.\n\n" +

            "5. Si estás en otra red, conecta Tailscale en la PC y en el teléfono.\n\n" +

            "6. Abre Configuración en RemoControl Mobile.\n\n" +

            "7. Copia la dirección del servidor exactamente como aparece en la PC.\n\n" +

            "8. Copia también el token de seguridad.\n\n" +

            "9. Pulsa Probar conexión.\n\n" +

            "10. Cuando aparezca Conexión correcta, guarda los cambios.\n\n" +

            "Si no conecta, comprueba que RemoControl Server esté iniciado, que el token sea correcto y que Tailscale esté conectado cuando uses acceso remoto.",
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