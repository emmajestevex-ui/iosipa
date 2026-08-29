namespace RemoControlMobile;

public partial class EquiposPage : ContentPage
{
    private List<EquipoConfig> equipos =
        new();


    public EquiposPage()
    {
        InitializeComponent();

        Cargar();
    }


    private void Cargar()
    {
        equipos =
            EquiposService.Obtener();

        lista.ItemsSource =
            equipos;
    }


    private async void BtnAgregar_Clicked(
        object sender,
        EventArgs e)
    {
        string? nombre =
            await DisplayPromptAsync(
                "Guardar PC",
                "Nombre del equipo:");

        if (string.IsNullOrWhiteSpace(
            nombre))
        {
            return;
        }

        equipos.Add(
            new EquipoConfig
            {
                Nombre =
                    nombre.Trim(),

                Servidor =
                    AppConfig.Servidor,

                Token =
                    AppConfig.Token
            });

        EquiposService.Guardar(
            equipos);

        Cargar();
    }


    private async void Lista_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        EquipoConfig? equipo =
            e.CurrentSelection
                .FirstOrDefault()
                as EquipoConfig;

        lista.SelectedItem =
            null;

        if (equipo == null)
        {
            return;
        }

        bool activar =
            await DisplayAlertAsync(
                equipo.Nombre,
                "¿Conectarte a esta PC?",
                "Conectar",
                "Cancelar");

        if (!activar)
        {
            return;
        }

        EquiposService.Activar(
            equipo);

        await Navigation.PopAsync();
    }


    private async void BtnVolver_Clicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}