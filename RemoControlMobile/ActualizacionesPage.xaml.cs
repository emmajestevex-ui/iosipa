namespace RemoControlMobile;

public partial class ActualizacionesPage : ContentPage
{
    private UpdateInfo? actualizacion;

    private CancellationTokenSource?
        cancelacionDescarga;


    public ActualizacionesPage()
    {
        InitializeComponent();

        lblVersionActual.Text =
            "Versión " +
            AppInfo.Current
                .VersionString;

        lblBuildActual.Text =
            "Build " +
            AppInfo.Current
                .BuildString;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Buscar();
    }


    // ============================================================
    // BUSCAR ACTUALIZACIÓN
    // ============================================================

    private async Task Buscar()
    {
        try
        {
            actualizacion =
                null;

            btnActualizar.IsVisible =
                false;

            panelDescarga.IsVisible =
                false;

            lblVersionNueva.Text =
                "Consultando...";

            lblVersionNueva.TextColor =
                Colors.Orange;

            lblEstado.Text =
                "";

            lblNotas.Text =
                "Consultando GitHub...";

            UpdateInfo? info =
                await UpdateService
                    .BuscarActualizacion();

            if (info == null)
            {
                lblVersionNueva.Text =
                    "No disponible";

                lblVersionNueva.TextColor =
                    Colors.Red;

                lblEstado.Text =
                    "No se pudo consultar GitHub.";

                lblEstado.TextColor =
                    Colors.Red;

                lblNotas.Text =
                    "--";

                return;
            }

            actualizacion =
                info;

            lblVersionNueva.Text =
                "Versión " +
                (
                    info.versionName ??
                    "desconocida"
                );

            lblNotas.Text =
                string.IsNullOrWhiteSpace(
                    info.notes)
                    ? "Sin notas de versión."
                    : info.notes;

            bool hayNueva =
                UpdateService
                    .HayActualizacion(
                        info);

            if (hayNueva)
            {
                lblVersionNueva.TextColor =
                    Colors.LimeGreen;

                lblEstado.Text =
                    "● Nueva actualización disponible";

                lblEstado.TextColor =
                    Colors.LimeGreen;

                btnActualizar.IsVisible =
                    true;
            }
            else
            {
                lblVersionNueva.TextColor =
                    Colors.White;

                lblEstado.Text =
                    "● Tienes la versión más reciente";

                lblEstado.TextColor =
                    Colors.LimeGreen;

                btnActualizar.IsVisible =
                    false;
            }
        }
        catch
        {
            lblVersionNueva.Text =
                "Error";

            lblVersionNueva.TextColor =
                Colors.Red;

            lblEstado.Text =
                "No se pudo comprobar la actualización.";

            lblEstado.TextColor =
                Colors.Red;

            lblNotas.Text =
                "--";
        }
    }


    // ============================================================
    // ACTUALIZAR
    // ============================================================

    private async void BtnActualizarAhora_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            actualizacion == null ||
            string.IsNullOrWhiteSpace(
                actualizacion.apkUrl))
        {
            await DisplayAlertAsync(
                "Actualización",
                "No hay un APK disponible.",
                "Aceptar");

            return;
        }

        bool continuar =
            await DisplayAlertAsync(
                "Actualizar RemoControl",
                "¿Descargar e instalar la versión " +
                (
                    actualizacion.versionName ??
                    ""
                ) +
                "?",
                "Actualizar",
                "Cancelar");

        if (!continuar)
        {
            return;
        }

        await DescargarActualizacion(
            actualizacion);
    }


    // ============================================================
    // DESCARGAR APK
    // ============================================================

    private async Task DescargarActualizacion(
        UpdateInfo info)
    {
        try
        {
            try
            {
                cancelacionDescarga?
                    .Cancel();

                cancelacionDescarga?
                    .Dispose();
            }
            catch
            {
            }

            cancelacionDescarga =
                new CancellationTokenSource();

            CancellationToken token =
                cancelacionDescarga.Token;

            panelDescarga.IsVisible =
                true;

            btnActualizar.IsEnabled =
                false;

            barraDescarga.Progress =
                0;

            lblProgreso.Text =
                "0 %";

            lblDescarga.Text =
                "Descargando RemoControl " +
                (
                    info.versionName ??
                    ""
                );

            using HttpClient cliente =
                new HttpClient();

            cliente.Timeout =
                TimeSpan.FromMinutes(
                    10);

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    info.apkUrl,
                    HttpCompletionOption
                        .ResponseHeadersRead,
                    token);

            respuesta
                .EnsureSuccessStatusCode();

            long total =
                respuesta.Content
                    .Headers
                    .ContentLength ??
                0;

            string ruta =
                Path.Combine(
                    FileSystem
                        .CacheDirectory,
                    "RemoControlUpdate.apk");

            using Stream entrada =
                await respuesta.Content
                    .ReadAsStreamAsync(
                        token);

            using FileStream salida =
                new FileStream(
                    ruta,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);

            byte[] buffer =
                new byte[
                    64 * 1024];

            long descargados =
                0;

            int leidos;

            while (
                (leidos =
                    await entrada.ReadAsync(
                        buffer,
                        token)) > 0)
            {
                await salida.WriteAsync(
                    buffer.AsMemory(
                        0,
                        leidos),
                    token);

                descargados +=
                    leidos;

                if (total > 0)
                {
                    double porcentaje =
                        (double)descargados /
                        total;

                    barraDescarga.Progress =
                        Math.Clamp(
                            porcentaje,
                            0,
                            1);

                    lblProgreso.Text =
                        Math.Round(
                            porcentaje *
                            100) +
                        " %";
                }
            }

            barraDescarga.Progress =
                1;

            lblProgreso.Text =
                "100 %";

            lblDescarga.Text =
                "Descarga completada";

            NotificationService.Mostrar(
                "Actualización",
                "La nueva versión está lista para instalar.");

            bool instalar =
                await DisplayAlertAsync(
                    "Actualización lista",
                    "La descarga terminó correctamente.",
                    "Instalar",
                    "Después");

            if (instalar)
            {
                ApkInstaller.Instalar(
                    ruta);
            }
        }
        catch (OperationCanceledException)
        {
            lblDescarga.Text =
                "Descarga cancelada";

            lblProgreso.Text =
                "Cancelada";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Actualización",
                ex.Message,
                "Aceptar");
        }
        finally
        {
            btnActualizar.IsEnabled =
                true;
        }
    }


    // ============================================================
    // CANCELAR
    // ============================================================

    private void BtnCancelarDescarga_Clicked(
        object sender,
        EventArgs e)
    {
        cancelacionDescarga?
            .Cancel();
    }


    // ============================================================
    // BUSCAR MANUALMENTE
    // ============================================================

    private async void BtnBuscar_Clicked(
        object sender,
        EventArgs e)
    {
        await Buscar();
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


    protected override void OnDisappearing()
    {
        try
        {
            cancelacionDescarga?
                .Cancel();
        }
        catch
        {
        }

        base.OnDisappearing();
    }
}