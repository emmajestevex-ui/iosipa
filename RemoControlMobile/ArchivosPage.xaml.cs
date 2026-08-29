using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RemoControlMobile;

public partial class ArchivosPage : ContentPage
{
    private readonly HttpClient cliente =
        AppConfig.CrearCliente(30);

    private string rutaActual =
        "";

    private CancellationTokenSource?
        cancelacionTransferencia;


    public ArchivosPage()
    {
        InitializeComponent();

        Appearing +=
            ArchivosPage_Appearing;
    }


    // ============================================================
    // AL ABRIR LA PÁGINA
    // ============================================================

    private async void ArchivosPage_Appearing(
        object? sender,
        EventArgs e)
    {
        if (string.IsNullOrEmpty(
            rutaActual))
        {
            await CargarArchivos(
                "");
        }
    }


    // ============================================================
    // CARGAR ARCHIVOS
    // ============================================================

    private async Task CargarArchivos(
        string ruta)
    {
        try
        {
            cargando.IsRunning =
                true;

            string url =
                AppConfig.Servidor +
                "/files";

            if (!string.IsNullOrWhiteSpace(
                ruta))
            {
                url +=
                    "?path=" +
                    Uri.EscapeDataString(
                        ruta);
            }

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    url);

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se pudo abrir esa ubicación.",
                    "Aceptar");

                return;
            }

            RespuestaArchivos? datos =
                await respuesta.Content
                    .ReadFromJsonAsync
                    <RespuestaArchivos>();

            if (datos == null)
            {
                return;
            }

            rutaActual =
                datos.path ==
                "Este equipo"
                    ? ""
                    : datos.path ??
                      "";

            lblRuta.Text =
                string.IsNullOrEmpty(
                    rutaActual)
                    ? "Este equipo"
                    : rutaActual;

            if (datos.items == null)
            {
                listaArchivos.ItemsSource =
                    null;

                return;
            }

            foreach (
                ArchivoRemoto item
                in datos.items)
            {
                PrepararElemento(
                    item);
            }

            listaArchivos.ItemsSource =
                datos.items;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                ex.Message,
                "Aceptar");
        }
        finally
        {
            cargando.IsRunning =
                false;

            refreshArchivos.IsRefreshing =
                false;
        }
    }


    // ============================================================
    // PREPARAR ELEMENTO
    // ============================================================

    private void PrepararElemento(
        ArchivoRemoto item)
    {
        if (item.type == "drive")
        {
            item.Icono =
                "💽";

            item.Detalle =
                FormatearBytes(
                    item.free) +
                " libres de " +
                FormatearBytes(
                    item.total);

            item.Indicador =
                "›";
        }
        else if (item.type == "folder")
        {
            item.Icono =
                "📁";

            item.Detalle =
                "Carpeta";

            item.Indicador =
                "›";
        }
        else
        {
            item.Icono =
                ObtenerIconoArchivo(
                    item.name);

            item.Detalle =
                FormatearBytes(
                    item.size);

            item.Indicador =
                "";
        }
    }


    // ============================================================
    // SELECCIONAR ELEMENTO
    // ============================================================

    private async void ListaArchivos_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        ArchivoRemoto? seleccionado =
            e.CurrentSelection
                .FirstOrDefault()
                as ArchivoRemoto;

        if (seleccionado == null)
        {
            return;
        }

        listaArchivos.SelectedItem =
            null;

        if (
            seleccionado.type ==
            "drive" ||
            seleccionado.type ==
            "folder")
        {
            await CargarArchivos(
                seleccionado.path ??
                "");

            return;
        }

        string? accion =
            await DisplayActionSheetAsync(
                seleccionado.name ??
                "Archivo",
                "Cancelar",
                null,
                "Descargar");

        if (accion == "Descargar")
        {
            await DescargarArchivo(
                seleccionado);
        }
    }


    // ============================================================
    // DESCARGAR ARCHIVO
    // ============================================================

    private async Task DescargarArchivo(
        ArchivoRemoto archivo)
    {
        if (string.IsNullOrWhiteSpace(
            archivo.path))
        {
            return;
        }

        try
        {
            PrepararTransferencia();

            CancellationToken token =
                cancelacionTransferencia!
                    .Token;

            panelTransferencia.IsVisible =
                true;

            barraTransferencia.Progress =
                0;

            lblPorcentaje.Text =
                "0 %";

            lblTransferencia.Text =
                "Descargando " +
                (
                    archivo.name ??
                    "archivo"
                );

            string url =
                AppConfig.Servidor +
                "/download?path=" +
                Uri.EscapeDataString(
                    archivo.path);

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    url,
                    HttpCompletionOption
                        .ResponseHeadersRead,
                    token);

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se pudo descargar.",
                    "Aceptar");

                return;
            }

            long total =
                respuesta.Content
                    .Headers
                    .ContentLength ??
                0;

            string nombre =
                archivo.name ??
                "archivo";

            string ruta =
                Path.Combine(
                    FileSystem
                        .CacheDirectory,
                    nombre);

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

            long recibidos =
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

                recibidos +=
                    leidos;

                if (total > 0)
                {
                    double porcentaje =
                        (double)recibidos /
                        total;

                    ActualizarProgreso(
                        porcentaje);
                }
            }

            ActualizarProgreso(
                1);

            lblTransferencia.Text =
                "Descarga completada";

            NotificationService.Mostrar(
                "RemoControl",
                nombre +
                " descargado.");

            bool compartir =
                await DisplayAlertAsync(
                    "Descargado",
                    nombre +
                    "\n\n¿Quieres abrir o compartir el archivo?",
                    "Compartir",
                    "Cerrar");

            if (compartir)
            {
                await Share.Default
                    .RequestAsync(
                        new ShareFileRequest
                        {
                            Title =
                                nombre,

                            File =
                                new ShareFile(
                                    ruta)
                        });
            }
        }
        catch (OperationCanceledException)
        {
            lblTransferencia.Text =
                "Descarga cancelada";

            lblPorcentaje.Text =
                "Cancelada";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                ex.Message,
                "Aceptar");
        }
    }


    // ============================================================
    // SUBIR ARCHIVO
    // ============================================================

    private async void BtnSubirArchivo_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                rutaActual))
            {
                await DisplayAlertAsync(
                    "Subir archivo",
                    "Primero entra en una carpeta de la PC.",
                    "Aceptar");

                return;
            }

            FileResult? resultado =
                await FilePicker.Default
                    .PickAsync(
                        new PickOptions
                        {
                            PickerTitle =
                                "Selecciona un archivo"
                        });

            if (resultado == null)
            {
                return;
            }

            bool continuar =
                await DisplayAlertAsync(
                    "Subir archivo",
                    "¿Subir \"" +
                    resultado.FileName +
                    "\" a:\n\n" +
                    rutaActual +
                    "?",
                    "Subir",
                    "Cancelar");

            if (!continuar)
            {
                return;
            }

            PrepararTransferencia();

            CancellationToken token =
                cancelacionTransferencia!
                    .Token;

            panelTransferencia.IsVisible =
                true;

            barraTransferencia.Progress =
                0;

            lblPorcentaje.Text =
                "0 %";

            lblTransferencia.Text =
                "Subiendo " +
                resultado.FileName;

            Stream stream =
                await resultado
                    .OpenReadAsync();

            ProgressStreamContent contenido =
                new ProgressStreamContent(
                    stream,
                    (enviados, total) =>
                    {
                        if (total <= 0)
                        {
                            return;
                        }

                        double porcentaje =
                            (double)enviados /
                            total;

                        MainThread
                            .BeginInvokeOnMainThread(
                                () =>
                                {
                                    ActualizarProgreso(
                                        porcentaje);
                                });
                    });

            contenido.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "application/octet-stream");

            string url =
                AppConfig.Servidor +
                "/upload?path=" +
                Uri.EscapeDataString(
                    rutaActual);

            using HttpRequestMessage peticion =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url);

            peticion.Headers.Add(
                "X-File-Name",
                resultado.FileName);

            peticion.Content =
                contenido;

            using HttpResponseMessage respuesta =
                await cliente.SendAsync(
                    peticion,
                    HttpCompletionOption
                        .ResponseHeadersRead,
                    token);

            if (
                respuesta.StatusCode ==
                System.Net
                    .HttpStatusCode
                    .Conflict)
            {
                await DisplayAlertAsync(
                    "Archivo existente",
                    "Ya existe un archivo con ese nombre en la PC.",
                    "Aceptar");

                return;
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se pudo subir el archivo.",
                    "Aceptar");

                return;
            }

            ActualizarProgreso(
                1);

            lblTransferencia.Text =
                "Transferencia completada";

            NotificationService.Mostrar(
                "RemoControl",
                "Archivo subido correctamente.");

            await DisplayAlertAsync(
                "RemoControl",
                "Archivo subido correctamente.",
                "Aceptar");

            await CargarArchivos(
                rutaActual);
        }
        catch (OperationCanceledException)
        {
            lblTransferencia.Text =
                "Transferencia cancelada";

            lblPorcentaje.Text =
                "Cancelada";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Error",
                ex.Message,
                "Aceptar");
        }
    }


    // ============================================================
    // PREPARAR TRANSFERENCIA
    // ============================================================

    private void PrepararTransferencia()
    {
        try
        {
            cancelacionTransferencia?
                .Cancel();

            cancelacionTransferencia?
                .Dispose();
        }
        catch
        {
        }

        cancelacionTransferencia =
            new CancellationTokenSource();

        panelTransferencia.IsVisible =
            true;

        barraTransferencia.Progress =
            0;

        lblPorcentaje.Text =
            "0 %";

        lblTransferencia.Text =
            "Preparando...";
    }


    // ============================================================
    // ACTUALIZAR PROGRESO
    // ============================================================

    private void ActualizarProgreso(
        double progreso)
    {
        progreso =
            Math.Clamp(
                progreso,
                0,
                1);

        barraTransferencia.Progress =
            progreso;

        lblPorcentaje.Text =
            Math.Round(
                progreso *
                100) +
            " %";
    }


    // ============================================================
    // CANCELAR
    // ============================================================

    private void BtnCancelarTransferencia_Clicked(
        object sender,
        EventArgs e)
    {
        cancelacionTransferencia?
            .Cancel();
    }


    // ============================================================
    // SUBIR FOTO / VIDEO
    // ============================================================

    private async void BtnSubirFoto_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                rutaActual))
            {
                await DisplayAlertAsync(
                    "RemoControl",
                    "Primero entra en una carpeta de la PC.",
                    "Aceptar");

                return;
            }

            FileResult? foto =
                await MediaPicker.Default
                    .PickPhotoAsync();

            if (foto == null)
            {
                return;
            }

            await SubirResultado(
                foto);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Foto",
                ex.Message,
                "Aceptar");
        }
    }


    private async void BtnSubirVideo_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                rutaActual))
            {
                await DisplayAlertAsync(
                    "RemoControl",
                    "Primero entra en una carpeta de la PC.",
                    "Aceptar");

                return;
            }

            FileResult? video =
                await MediaPicker.Default
                    .PickVideoAsync();

            if (video == null)
            {
                return;
            }

            await SubirResultado(
                video);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Video",
                ex.Message,
                "Aceptar");
        }
    }


    // ============================================================
    // SUBIR FILE RESULT
    // ============================================================

    private async Task SubirResultado(
        FileResult resultado)
    {
        try
        {
            PrepararTransferencia();

            CancellationToken token =
                cancelacionTransferencia!
                    .Token;

            lblTransferencia.Text =
                "Subiendo " +
                resultado.FileName;

            Stream stream =
                await resultado
                    .OpenReadAsync();

            ProgressStreamContent contenido =
                new ProgressStreamContent(
                    stream,
                    (enviados, total) =>
                    {
                        if (total <= 0)
                        {
                            return;
                        }

                        double porcentaje =
                            (double)enviados /
                            total;

                        MainThread
                            .BeginInvokeOnMainThread(
                                () =>
                                    ActualizarProgreso(
                                        porcentaje));
                    });

            contenido.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "application/octet-stream");

            string url =
                AppConfig.Servidor +
                "/upload?path=" +
                Uri.EscapeDataString(
                    rutaActual);

            using HttpRequestMessage peticion =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url);

            peticion.Headers.Add(
                "X-File-Name",
                resultado.FileName);

            peticion.Content =
                contenido;

            using HttpResponseMessage respuesta =
                await cliente.SendAsync(
                    peticion,
                    HttpCompletionOption
                        .ResponseHeadersRead,
                    token);

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Error",
                    "No se pudo subir.",
                    "Aceptar");

                return;
            }

            ActualizarProgreso(
                1);

            lblTransferencia.Text =
                "Transferencia completada";

            NotificationService.Mostrar(
                "RemoControl",
                resultado.FileName +
                " enviado a la PC.");

            await CargarArchivos(
                rutaActual);
        }
        catch (OperationCanceledException)
        {
            lblTransferencia.Text =
                "Transferencia cancelada";

            lblPorcentaje.Text =
                "Cancelada";
        }
    }


    // ============================================================
    // SUBIR TEXTO COMO TXT
    // ============================================================

    private async void BtnCrearTexto_Clicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(
            rutaActual))
        {
            await DisplayAlertAsync(
                "Texto",
                "Primero entra en una carpeta de la PC.",
                "Aceptar");

            return;
        }

        string? nombre =
            await DisplayPromptAsync(
                "Nuevo archivo",
                "Nombre del archivo:",
                initialValue:
                    "nota.txt");

        if (string.IsNullOrWhiteSpace(
            nombre))
        {
            return;
        }

        if (!nombre.EndsWith(
            ".txt",
            StringComparison
                .OrdinalIgnoreCase))
        {
            nombre +=
                ".txt";
        }

        string? texto =
            await DisplayPromptAsync(
                "Contenido",
                "Escribe el texto:");

        if (texto == null)
        {
            return;
        }

        string temporal =
            Path.Combine(
                FileSystem.CacheDirectory,
                nombre);

        await File.WriteAllTextAsync(
            temporal,
            texto);

        FileResult archivo =
            new FileResult(
                temporal);

        await SubirResultado(
            archivo);
    }


    // ============================================================
    // ATRÁS EN CARPETAS
    // ============================================================

    private async void BtnAtrasCarpeta_Clicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrEmpty(
            rutaActual))
        {
            return;
        }

        string ruta =
            rutaActual.TrimEnd(
                '\\',
                '/');

        int posicion =
            ruta.LastIndexOf(
                '\\');

        if (posicion <= 2)
        {
            await CargarArchivos(
                "");

            return;
        }

        await CargarArchivos(
            ruta.Substring(
                0,
                posicion));
    }


    // ============================================================
    // ACTUALIZAR
    // ============================================================

    private async void BtnActualizar_Clicked(
        object sender,
        EventArgs e)
    {
        await CargarArchivos(
            rutaActual);
    }


    // ============================================================
    // REFRESH
    // ============================================================

    private async void RefreshArchivos_Refreshing(
        object sender,
        EventArgs e)
    {
        await CargarArchivos(
            rutaActual);
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


    // ============================================================
    // FORMATO DE BYTES
    // ============================================================

    private static string FormatearBytes(
        long bytes)
    {
        if (bytes < 1024)
        {
            return
                bytes +
                " B";
        }

        double kb =
            bytes /
            1024d;

        if (kb < 1024)
        {
            return
                kb.ToString(
                    "0.0") +
                " KB";
        }

        double mb =
            kb /
            1024d;

        if (mb < 1024)
        {
            return
                mb.ToString(
                    "0.0") +
                " MB";
        }

        double gb =
            mb /
            1024d;

        return
            gb.ToString(
                "0.0") +
            " GB";
    }


    // ============================================================
    // ICONOS
    // ============================================================

    private static string ObtenerIconoArchivo(
        string? nombre)
    {
        string extension =
            Path.GetExtension(
                nombre ??
                "")
            .ToLowerInvariant();

        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
            case ".png":
            case ".webp":
            case ".gif":

                return "🖼️";


            case ".mp4":
            case ".mkv":
            case ".avi":
            case ".mov":

                return "🎬";


            case ".mp3":
            case ".wav":
            case ".flac":
            case ".aac":

                return "🎵";


            case ".pdf":

                return "📕";


            case ".zip":
            case ".rar":
            case ".7z":

                return "📦";


            case ".txt":
            case ".log":

                return "📝";


            case ".exe":

                return "⚙️";


            default:

                return "📄";
        }
    }


    // ============================================================
    // LIMPIAR RECURSOS
    // ============================================================

    protected override void OnDisappearing()
    {
        try
        {
            cancelacionTransferencia?
                .Cancel();
        }
        catch
        {
        }

        base.OnDisappearing();
    }
}


// ================================================================
// RESPUESTA DEL SERVIDOR
// ================================================================

public class RespuestaArchivos
{
    public bool ok
    {
        get;
        set;
    }

    public string? path
    {
        get;
        set;
    }

    public List<ArchivoRemoto>? items
    {
        get;
        set;
    }
}


// ================================================================
// ARCHIVO REMOTO
// ================================================================

public class ArchivoRemoto
{
    public string? name
    {
        get;
        set;
    }

    public string? path
    {
        get;
        set;
    }

    public string? type
    {
        get;
        set;
    }

    public long size
    {
        get;
        set;
    }

    public long free
    {
        get;
        set;
    }

    public long total
    {
        get;
        set;
    }

    public string Icono
    {
        get;
        set;
    } = "📄";

    public string Detalle
    {
        get;
        set;
    } = "";

    public string Indicador
    {
        get;
        set;
    } = "";
}