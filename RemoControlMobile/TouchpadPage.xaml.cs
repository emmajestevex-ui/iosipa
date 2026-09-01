using System.Globalization;

namespace RemoControlMobile;

public partial class TouchpadPage : ContentPage
{
    private double x = 0.5;
    private double y = 0.5;

    private double inicioX;
    private double inicioY;

    private DateTime ultimoMovimiento =
        DateTime.MinValue;


    public TouchpadPage()
    {
        InitializeComponent();
    }


    private async Task Post(
        string endpoint)
    {
        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(8);

            using HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    AppConfig.Servidor +
                    endpoint,
                    null);

            lblEstado.Text =
                respuesta.IsSuccessStatusCode
                    ? "● Conectado"
                    : "Error";

            lblEstado.TextColor =
                respuesta.IsSuccessStatusCode
                    ? Colors.LimeGreen
                    : Colors.Red;
        }
        catch
        {
            lblEstado.Text =
                "Sin conexión";

            lblEstado.TextColor =
                Colors.Red;
        }
    }


    private async void Touchpad_PanUpdated(
        object sender,
        PanUpdatedEventArgs e)
    {
        if (
            e.StatusType ==
            GestureStatus.Started)
        {
            inicioX = x;
            inicioY = y;

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

        double ancho =
            Math.Max(
                zonaTouchpad.Width,
                1);

        double alto =
            Math.Max(
                zonaTouchpad.Height,
                1);

        x =
            Math.Clamp(
                inicioX +
                e.TotalX / ancho,
                0,
                1);

        y =
            Math.Clamp(
                inicioY +
                e.TotalY / alto,
                0,
                1);

        string sx =
            x.ToString(
                CultureInfo.InvariantCulture);

        string sy =
            y.ToString(
                CultureInfo.InvariantCulture);

        await Post(
            "/input/move?x=" +
            Uri.EscapeDataString(sx) +
            "&y=" +
            Uri.EscapeDataString(sy));
    }


    private async void Touchpad_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await EnviarClick(
            "/input/click");
    }


    private async void Touchpad_DoubleTapped(
        object sender,
        TappedEventArgs e)
    {
        await EnviarClick(
            "/input/doubleclick");
    }


    private async Task EnviarClick(
        string endpoint)
    {
        string sx =
            x.ToString(
                CultureInfo.InvariantCulture);

        string sy =
            y.ToString(
                CultureInfo.InvariantCulture);

        await Post(
            endpoint +
            "?x=" +
            Uri.EscapeDataString(sx) +
            "&y=" +
            Uri.EscapeDataString(sy));
    }


    private async void BtnIzquierdo_Clicked(
        object sender,
        EventArgs e)
    {
        await EnviarClick(
            "/input/click");
    }


    private async void BtnDerecho_Clicked(
        object sender,
        EventArgs e)
    {
        await EnviarClick(
            "/input/rightclick");
    }


    private async void BtnScrollArriba_Clicked(
        object sender,
        EventArgs e)
    {
        await Post(
            "/input/scroll?delta=120");
    }


    private async void BtnScrollAbajo_Clicked(
        object sender,
        EventArgs e)
    {
        await Post(
            "/input/scroll?delta=-120");
    }


    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}