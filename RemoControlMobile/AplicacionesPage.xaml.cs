using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace RemoControlMobile;

public partial class AplicacionesPage : ContentPage
{
    private List<AplicacionRemota> aplicaciones =
        new();

    public AplicacionesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarAplicaciones();
    }

    // ============================================================
    // CARGAR SELECCIONADAS
    // ============================================================

    private async Task CargarAplicaciones()
    {
        try
        {
            lblEstado.Text =
                "● Cargando...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(
                    20);

            AplicacionesRespuesta? datos =
                await cliente
                    .GetFromJsonAsync
                    <AplicacionesRespuesta>(
                        AppConfig.Servidor +
                        "/apps");

            aplicaciones =
                datos?.items ??
                new List<AplicacionRemota>();

            PrepararIconos(
                aplicaciones);

            MostrarLista(
                aplicaciones);

            lblEstado.Text =
                "● " +
                aplicaciones.Count +
                " aplicación(es)";

            lblEstado.TextColor =
                Colors.LimeGreen;
        }
        catch
        {
            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;
        }
        finally
        {
            refreshApps.IsRefreshing =
                false;
        }
    }

    // ============================================================
    // ICONOS
    // ============================================================

    private void PrepararIconos(
        IEnumerable<AplicacionRemota> lista)
    {
        foreach (AplicacionRemota app in lista)
        {
            app.IconImage =
                ConvertirBase64AImagen(
                    app.iconBase64);
        }
    }

    private void PrepararIconosDisponibles(
        IEnumerable<AppDisponible> lista)
    {
        foreach (AppDisponible app in lista)
        {
            app.IconImage =
                ConvertirBase64AImagen(
                    app.iconBase64);
        }
    }

    private ImageSource ConvertirBase64AImagen(
        string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return CrearIconoFallback();
        }

        try
        {
            byte[] bytes =
                Convert.FromBase64String(
                    base64);

            return ImageSource.FromStream(
                () =>
                    new MemoryStream(
                        bytes));
        }
        catch
        {
            return CrearIconoFallback();
        }
    }

    private ImageSource CrearIconoFallback()
    {
        return new FontImageSource
        {
            Glyph = "●",
            Size = 30,
            Color = Colors.SlateGray
        };
    }

    // ============================================================
    // AGREGAR
    // ============================================================

    private async void BtnAgregar_Clicked(
        object sender,
        EventArgs e)
    {
        try
        {
            lblEstado.Text =
                "● Buscando aplicaciones instaladas...";

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(
                    30);

            AppsDisponiblesRespuesta? datos =
                await cliente
                    .GetFromJsonAsync
                    <AppsDisponiblesRespuesta>(
                        AppConfig.Servidor +
                        "/apps/discover");

            if (
                datos?.items == null ||
                datos.items.Count == 0)
            {
                await DisplayAlertAsync(
                    "Aplicaciones",
                    "No se encontraron programas instalados para mostrar.",
                    "Aceptar");

                lblEstado.Text =
                    "● Sin aplicaciones";

                return;
            }

            List<AppDisponible> disponibles =
                datos.items
                    .Where(
                        x => !x.added)
                    .OrderBy(
                        x => x.name)
                    .ToList();

            if (disponibles.Count == 0)
            {
                await DisplayAlertAsync(
                    "Aplicaciones",
                    "Ya agregaste todas las aplicaciones disponibles.",
                    "Aceptar");

                lblEstado.Text =
                    "● Todo agregado";

                lblEstado.TextColor =
                    Colors.LimeGreen;

                return;
            }

            PrepararIconosDisponibles(
                disponibles);

            await Navigation.PushAsync(
                new SeleccionarAplicacionesPage(
                    disponibles,
                    AplicacionesAgregadasDesdeSelector));
        }
        catch
        {
            await DisplayAlertAsync(
                "Aplicaciones",
                "No se pudo obtener la lista de aplicaciones de la PC.",
                "Aceptar");

            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;
        }
    }

    private async Task AplicacionesAgregadasDesdeSelector()
    {
        await CargarAplicaciones();
    }

    // ============================================================
    // OPCIONES
    // ============================================================

    private async void ListaApps_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        AplicacionRemota? app =
            e.CurrentSelection
                .FirstOrDefault()
                as AplicacionRemota;

        listaApps.SelectedItem =
            null;

        if (app == null)
        {
            return;
        }

        string textoFavorito =
            app.favorite
                ? "Quitar de favoritos"
                : "⭐ Agregar a favoritos";

        string? opcion =
            await DisplayActionSheetAsync(
                app.name ??
                "Aplicación",
                "Cancelar",
                "Eliminar",
                "▶ Abrir",
                textoFavorito);

        if (opcion == "▶ Abrir")
        {
            await Abrir(
                app);
        }
        else if (opcion == textoFavorito)
        {
            await Favorito(
                app);
        }
        else if (opcion == "Eliminar")
        {
            await Eliminar(
                app);
        }
    }

    // ============================================================
    // ABRIR
    // ============================================================

    private async Task Abrir(
        AplicacionRemota app)
    {
        try
        {
            lblEstado.Text =
                "● Abriendo " +
                (
                    app.name ??
                    ""
                );

            lblEstado.TextColor =
                Colors.Orange;

            using HttpClient cliente =
                AppConfig.CrearCliente(
                    10);

            string url =
                AppConfig.Servidor +
                "/apps/open?id=" +
                Uri.EscapeDataString(
                    app.id ??
                    "");

            using HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    url,
                    null);

            if (!respuesta.IsSuccessStatusCode)
            {
                string detalle =
                    await respuesta.Content
                        .ReadAsStringAsync();

                await DisplayAlertAsync(
                    "Aplicaciones",
                    "No se pudo abrir la aplicación.\n\n" +
                    detalle,
                    "Aceptar");

                lblEstado.Text =
                    "● Error al abrir";

                lblEstado.TextColor =
                    Colors.Red;

                return;
            }

            lblEstado.Text =
                "● " +
                (
                    app.name ??
                    "Aplicación"
                ) +
                " abierta";

            lblEstado.TextColor =
                Colors.LimeGreen;
        }
        catch
        {
            await DisplayAlertAsync(
                "Aplicaciones",
                "Sin conexión con la PC.",
                "Aceptar");

            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;
        }
    }

    // ============================================================
    // FAVORITO
    // ============================================================

    private async Task Favorito(
        AplicacionRemota app)
    {
        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(
                    10);

            string url =
                AppConfig.Servidor +
                "/apps/favorite?id=" +
                Uri.EscapeDataString(
                    app.id ??
                    "");

            using HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    url,
                    null);

            if (respuesta.IsSuccessStatusCode)
            {
                await CargarAplicaciones();
            }
        }
        catch
        {
            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;
        }
    }

    // ============================================================
    // ELIMINAR
    // ============================================================

    private async Task Eliminar(
        AplicacionRemota app)
    {
        bool confirmar =
            await DisplayAlertAsync(
                "Eliminar",
                "¿Quitar " +
                app.name +
                " de RemoControl?\n\n" +
                "La aplicación no se desinstalará de la PC.",
                "Quitar",
                "Cancelar");

        if (!confirmar)
        {
            return;
        }

        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(
                    10);

            string url =
                AppConfig.Servidor +
                "/apps/remove?id=" +
                Uri.EscapeDataString(
                    app.id ??
                    "");

            using HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    url,
                    null);

            if (respuesta.IsSuccessStatusCode)
            {
                await CargarAplicaciones();
            }
        }
        catch
        {
            lblEstado.Text =
                "● Sin conexión";

            lblEstado.TextColor =
                Colors.Red;
        }
    }

    // ============================================================
    // BUSCAR
    // ============================================================

    private void TxtBuscar_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        string texto =
            e.NewTextValue ??
            "";

        if (string.IsNullOrWhiteSpace(texto))
        {
            MostrarLista(
                aplicaciones);

            return;
        }

        List<AplicacionRemota> filtradas =
            aplicaciones
                .Where(
                    x =>
                        (
                            x.name ??
                            ""
                        )
                        .Contains(
                            texto,
                            StringComparison.OrdinalIgnoreCase))
                .ToList();

        MostrarLista(
            filtradas);
    }

    private void MostrarLista(
        List<AplicacionRemota> lista)
    {
        listaApps.ItemsSource =
            null;

        listaApps.ItemsSource =
            lista;
    }

    // ============================================================
    // ACTUALIZAR
    // ============================================================

    private async void RefreshApps_Refreshing(
        object sender,
        EventArgs e)
    {
        await CargarAplicaciones();
    }

    private async void BtnActualizar_Clicked(
        object sender,
        EventArgs e)
    {
        await CargarAplicaciones();
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
}

// ================================================================
// MODELOS JSON
// ================================================================

public class AplicacionesRespuesta
{
    public bool ok
    {
        get;
        set;
    }

    public List<AplicacionRemota>? items
    {
        get;
        set;
    }
}

public class AppsDisponiblesRespuesta
{
    public bool ok
    {
        get;
        set;
    }

    public List<AppDisponible>? items
    {
        get;
        set;
    }
}

public class AplicacionRemota
{
    public string? id
    {
        get;
        set;
    }

    public string? name
    {
        get;
        set;
    }

    public string? iconBase64
    {
        get;
        set;
    }

    public bool favorite
    {
        get;
        set;
    }

    public ImageSource? IconImage
    {
        get;
        set;
    }

    public string FavoriteIcon
    {
        get
        {
            return favorite
                ? "⭐"
                : "";
        }
    }
}

public class AppDisponible
{
    public string? id
    {
        get;
        set;
    }

    public string? name
    {
        get;
        set;
    }

    public string? iconBase64
    {
        get;
        set;
    }

    public bool added
    {
        get;
        set;
    }

    public ImageSource? IconImage
    {
        get;
        set;
    }

    public bool selected
    {
        get;
        set;
    }
}
