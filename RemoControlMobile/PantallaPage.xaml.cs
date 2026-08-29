using System.Globalization;
using System.Net.Http.Json;

namespace RemoControlMobile;

public partial class PantallaPage : ContentPage
{
    private readonly HttpClient cliente =
        AppConfig.CrearCliente(10);

    private CancellationTokenSource? cancelacionPantalla;

    private double anchoCaptura = 1920;
    private double altoCaptura = 1080;

    private double ultimoX = 0.5;
    private double ultimoY = 0.5;

    private double panInicioX = 0.5;
    private double panInicioY = 0.5;

    private DateTime ultimoMovimiento =
        DateTime.MinValue;
    private int calidadJpeg =
    65;

    private bool pantallaCompleta =
        false;


    public PantallaPage()
    {
        InitializeComponent();
        pickerCalidad.SelectedIndex = 1;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        MainActivity.Instancia?.PonerVertical();
#endif

        panelCargando.IsVisible = true;
        cargandoPantalla.IsRunning = true;

        await CargarResolucionPc();

        IniciarTransmision();
    }


    protected override void OnDisappearing()
    {
        DetenerTransmision();

#if ANDROID
        MainActivity.Instancia?.PonerVertical();
#endif

        base.OnDisappearing();
    }


    private async Task CargarResolucionPc()
    {
        try
        {
            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    AppConfig.Servidor +
                    "/info");

            if (!respuesta.IsSuccessStatusCode)
                return;

            InfoPantallaSimple? info =
                await respuesta.Content
                    .ReadFromJsonAsync<InfoPantallaSimple>();

            if (info == null)
                return;

            if (
                info.screenWidth > 0 &&
                info.screenHeight > 0)
            {
                anchoCaptura =
                    info.screenWidth;

                altoCaptura =
                    info.screenHeight;
            }
        }
        catch
        {
        }
    }
    private void PickerCalidad_SelectedIndexChanged(
    object sender,
    EventArgs e)
    {
        switch (
            pickerCalidad.SelectedIndex)
        {
            case 0:

                calidadJpeg =
                    35;

                break;

            case 2:

                calidadJpeg =
                    82;

                break;

            case 3:

                calidadJpeg =
                    95;

                break;

            default:

                calidadJpeg =
                    65;

                break;
        }
    }


    private void BtnPantallaCompleta_Clicked(
        object sender,
        EventArgs e)
    {
        pantallaCompleta =
            !pantallaCompleta;

        panelEncabezado.IsVisible =
            !pantallaCompleta;

        panelControles.IsVisible =
            !pantallaCompleta;

#if ANDROID
        if (pantallaCompleta)
        {
            MainActivity.Instancia?
                .PonerHorizontal();
        }
        else
        {
            MainActivity.Instancia?
                .PonerVertical();
        }
#endif
    }


    private async void BtnCaptura_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            string url =
                AppConfig.Servidor +
                "/screen?quality=95&t=" +
                DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds();

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    url);

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Captura",
                    "No se pudo obtener la captura.",
                    "Aceptar");

                return;
            }

            byte[] datos =
                await respuesta.Content
                    .ReadAsByteArrayAsync();

            string nombre =
                "RemoControl_" +
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss") +
                ".jpg";

            string ruta =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    nombre);

            await File.WriteAllBytesAsync(
                ruta,
                datos);

            bool compartir =
                await DisplayAlertAsync(
                    "Captura guardada",
                    "La captura se guardó en RemoControl.\n\n" +
                    "¿Quieres compartirla o guardarla en otra aplicación?",
                    "Compartir",
                    "Cerrar");

            if (compartir)
            {
                await Share.Default
                    .RequestAsync(
                        new ShareFileRequest
                        {
                            Title =
                                "Captura RemoControl",

                            File =
                                new ShareFile(
                                    ruta,
                                    "image/jpeg")
                        });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Captura",
                ex.Message,
                "Aceptar");
        }
    }


    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }


    private void IniciarTransmision()
    {
        DetenerTransmision();

        cancelacionPantalla =
            new CancellationTokenSource();

        _ = BuclePantalla(
            cancelacionPantalla.Token);
    }


    private void DetenerTransmision()
    {
        try
        {
            cancelacionPantalla?.Cancel();
            cancelacionPantalla?.Dispose();

            cancelacionPantalla = null;
        }
        catch
        {
        }
    }


    private async Task BuclePantalla(
        CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await ActualizarPantalla(
                    token);

                await Task.Delay(
                    AppConfig.IntervaloPantalla,
                    token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                try
                {
                    await Task.Delay(
                        800,
                        token);
                }
                catch
                {
                    break;
                }
            }
        }
    }


    private async Task ActualizarPantalla(
        CancellationToken token)
    {
        try
        {
            string url =
                AppConfig.Servidor +
                "/screen?t=" +
                DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds();

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    url,
                    token);

            if (!respuesta.IsSuccessStatusCode)
                return;

            byte[] datos =
                await respuesta.Content
                    .ReadAsByteArrayAsync(
                        token);

            if (datos.Length == 0)
                return;

            ImageSource nuevaImagen =
                ImageSource.FromStream(
                    () =>
                        new MemoryStream(datos));

            await MainThread.InvokeOnMainThreadAsync(
                () =>
                {
                    imgPantalla.Source =
                        nuevaImagen;

                    panelCargando.IsVisible =
                        false;

                    cargandoPantalla.IsRunning =
                        false;
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
        }
    }


    private async Task<bool> EnviarPost(
        string endpoint)
    {
        try
        {
            using HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    AppConfig.Servidor +
                    endpoint,
                    null);

            return respuesta.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }


    private bool ObtenerAreaImagen(
        out double anchoReal,
        out double altoReal,
        out double offsetX,
        out double offsetY)
    {
        anchoReal = 0;
        altoReal = 0;
        offsetX = 0;
        offsetY = 0;

        if (
            imgPantalla.Width <= 0 ||
            imgPantalla.Height <= 0)
        {
            return false;
        }

        double anchoControl =
            imgPantalla.Width;

        double altoControl =
            imgPantalla.Height;

        double proporcionImagen =
            anchoCaptura /
            altoCaptura;

        double proporcionControl =
            anchoControl /
            altoControl;

        if (
            proporcionImagen >
            proporcionControl)
        {
            anchoReal =
                anchoControl;

            altoReal =
                anchoControl /
                proporcionImagen;

            offsetY =
                (altoControl -
                 altoReal) /
                2.0;
        }
        else
        {
            altoReal =
                altoControl;

            anchoReal =
                altoControl *
                proporcionImagen;

            offsetX =
                (anchoControl -
                 anchoReal) /
                2.0;
        }

        return true;
    }


    private bool ObtenerCoordenadasPantalla(
        Point punto,
        out double x,
        out double y)
    {
        x = 0;
        y = 0;

        if (!ObtenerAreaImagen(
            out double anchoReal,
            out double altoReal,
            out double offsetX,
            out double offsetY))
        {
            return false;
        }

        double px =
            punto.X -
            offsetX;

        double py =
            punto.Y -
            offsetY;

        if (
            px < 0 ||
            py < 0 ||
            px > anchoReal ||
            py > altoReal)
        {
            return false;
        }

        x =
            px /
            anchoReal;

        y =
            py /
            altoReal;

        x =
            Math.Clamp(
                x,
                0,
                1);

        y =
            Math.Clamp(
                y,
                0,
                1);

        return true;
    }


    private async void Pantalla_Tapped(
        object sender,
        TappedEventArgs e)
    {
        try
        {
            Point? punto =
                e.GetPosition(
                    imgPantalla);

            if (punto == null)
                return;

            if (!ObtenerCoordenadasPantalla(
                punto.Value,
                out double x,
                out double y))
            {
                return;
            }

            ultimoX = x;
            ultimoY = y;

            string sx =
                x.ToString(
                    CultureInfo.InvariantCulture);

            string sy =
                y.ToString(
                    CultureInfo.InvariantCulture);

            await EnviarPost(
                "/input/click?x=" +
                Uri.EscapeDataString(sx) +
                "&y=" +
                Uri.EscapeDataString(sy));
        }
        catch
        {
        }
    }


    private async void Pantalla_DoubleTapped(
        object sender,
        TappedEventArgs e)
    {
        try
        {
            Point? punto =
                e.GetPosition(
                    imgPantalla);

            if (punto == null)
                return;

            if (!ObtenerCoordenadasPantalla(
                punto.Value,
                out double x,
                out double y))
            {
                return;
            }

            ultimoX = x;
            ultimoY = y;

            string sx =
                x.ToString(
                    CultureInfo.InvariantCulture);

            string sy =
                y.ToString(
                    CultureInfo.InvariantCulture);

            await EnviarPost(
                "/input/doubleclick?x=" +
                Uri.EscapeDataString(sx) +
                "&y=" +
                Uri.EscapeDataString(sy));
        }
        catch
        {
        }
    }


    private async void Pantalla_PanUpdated(
        object sender,
        PanUpdatedEventArgs e)
    {
        try
        {
            if (!ObtenerAreaImagen(
                out double anchoReal,
                out double altoReal,
                out _,
                out _))
            {
                return;
            }

            if (
                e.StatusType ==
                GestureStatus.Started)
            {
                panInicioX =
                    ultimoX;

                panInicioY =
                    ultimoY;

                return;
            }

            if (
                e.StatusType !=
                GestureStatus.Running)
            {
                return;
            }

            if (
                (DateTime.Now -
                 ultimoMovimiento)
                .TotalMilliseconds < 35)
            {
                return;
            }

            ultimoMovimiento =
                DateTime.Now;

            double nuevoX =
                panInicioX +
                (e.TotalX /
                 anchoReal);

            double nuevoY =
                panInicioY +
                (e.TotalY /
                 altoReal);

            nuevoX =
                Math.Clamp(
                    nuevoX,
                    0,
                    1);

            nuevoY =
                Math.Clamp(
                    nuevoY,
                    0,
                    1);

            ultimoX =
                nuevoX;

            ultimoY =
                nuevoY;

            string sx =
                nuevoX.ToString(
                    CultureInfo.InvariantCulture);

            string sy =
                nuevoY.ToString(
                    CultureInfo.InvariantCulture);

            await EnviarPost(
                "/input/move?x=" +
                Uri.EscapeDataString(sx) +
                "&y=" +
                Uri.EscapeDataString(sy));
        }
        catch
        {
        }
    }
}


public class InfoPantallaSimple
{
    public int screenWidth { get; set; }

    public int screenHeight { get; set; }
}