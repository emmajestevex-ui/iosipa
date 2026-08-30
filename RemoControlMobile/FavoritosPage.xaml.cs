namespace RemoControlMobile;

public partial class FavoritosPage : ContentPage
{
    public FavoritosPage()
    {
        InitializeComponent();
    }


    private async void BtnPantalla_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new PantallaPage());
    }


    private async void BtnPortapapeles_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new PortapapelesPage());
    }


    private async void BtnMultimedia_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new MultimediaPage());
    }


    private async void BtnEstado_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new EstadoPcPage());
    }


    private async void BtnArchivos_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new ArchivosPage());
    }


    private async void BtnBloquear_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar =
            await DisplayAlertAsync(
                "Bloquear PC",
                "¿Quieres bloquear Windows?",
                "Bloquear",
                "Cancelar");

        if (!confirmar)
        {
            return;
        }

        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(8);

            using HttpResponseMessage respuesta =
                await AppConfig.PostAsyncConToken(
                    cliente,
                    "/action/lock");

            if (!respuesta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "RemoControl",
                    "No se pudo bloquear la PC.",
                    "Aceptar");
            }
        }
        catch
        {
            await DisplayAlertAsync(
                "RemoControl",
                "Sin conexión con la PC.",
                "Aceptar");
        }
    }


    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
