using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace RemoControlMobile;

public partial class AplicacionesPage : ContentPage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private List<AplicacionRemota> aplicaciones =
        new();

    public AplicacionesPage()
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CargarAplicaciones();
    }

    private string U(string path)
    {
        return AppConfig.Url(
            path);
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

            aplicaciones = await ObtenerListaAsync<AplicacionRemota>(
                cliente,
                "/apps",
                "/applications",
                "/pro/apps");

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
        catch (Exception ex)
        {
            lblEstado.Text =
                "● " + Recortar(ex.Message, 70);

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
            base64 =
                base64
                    .Replace("\0", "")
                    .Trim();

            int comma =
                base64.IndexOf(',');

            if (base64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                base64 = base64[(comma + 1)..];

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

            List<AppDisponible> items = await ObtenerListaAsync<AppDisponible>(
                cliente,
                "/apps/discover",
                "/apps/installed",
                "/applications/discover",
                "/applications/installed",
                "/pro/apps/discover",
                "/pro/apps/installed");

            if (items.Count == 0)
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
                items
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
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Aplicaciones",
                "No se pudo obtener la lista de aplicaciones de la PC.\n\n" +
                Recortar(ex.Message, 450),
                "Aceptar");

            lblEstado.Text =
                "● Error al buscar aplicaciones";

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

            string id = Uri.EscapeDataString(app.id ?? "");
            await PostFirstAsync(
                cliente,
                "/apps/open?id=" + id,
                "/applications/open?id=" + id,
                "/pro/apps/open?id=" + id);

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
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Aplicaciones",
                "No se pudo abrir la aplicación.\n\n" +
                Recortar(ex.Message, 450),
                "Aceptar");

            lblEstado.Text =
                "● Error al abrir";

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

            string id = Uri.EscapeDataString(app.id ?? "");
            await PostFirstAsync(
                cliente,
                "/apps/favorite?id=" + id,
                "/applications/favorite?id=" + id,
                "/pro/apps/favorite?id=" + id);

            await CargarAplicaciones();
        }
        catch (Exception ex)
        {
            lblEstado.Text =
                "● " + Recortar(ex.Message, 70);

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

            string id = Uri.EscapeDataString(app.id ?? "");
            await PostFirstAsync(
                cliente,
                "/apps/remove?id=" + id,
                "/applications/remove?id=" + id,
                "/pro/apps/remove?id=" + id);

            await CargarAplicaciones();
        }
        catch (Exception ex)
        {
            lblEstado.Text =
                "● " + Recortar(ex.Message, 70);

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

    private async Task<List<T>> ObtenerListaAsync<T>(
        HttpClient cliente,
        params string[] rutas)
    {
        string json = await GetJsonFirstAsync(
            cliente,
            rutas);

        return LeerItems<T>(
            json);
    }

    private async Task<string> GetJsonFirstAsync(
        HttpClient cliente,
        params string[] rutas)
    {
        string ultimoError =
            "La PC no respondió.";

        foreach (string ruta in rutas.Distinct())
        {
            try
            {
                using HttpResponseMessage response =
                    await AppConfig.GetAsyncConToken(
                        cliente,
                        ruta);

                string body =
                    LimpiarJson(
                        await response.Content.ReadAsStringAsync());

                if (response.IsSuccessStatusCode)
                    return body;

                ultimoError =
                    ExtraerError(body);
            }
            catch (Exception ex)
            {
                ultimoError =
                    ex.Message;
            }
        }

        throw new InvalidOperationException(
            ultimoError);
    }

    private async Task PostFirstAsync(
        HttpClient cliente,
        params string[] rutas)
    {
        string ultimoError =
            "La PC no respondió.";

        foreach (string ruta in rutas.Distinct())
        {
            try
            {
                using HttpResponseMessage response =
                    await AppConfig.PostAsyncConToken(
                        cliente,
                        ruta);

                string body =
                    LimpiarJson(
                        await response.Content.ReadAsStringAsync());

                if (response.IsSuccessStatusCode)
                    return;

                ultimoError =
                    ExtraerError(body);
            }
            catch (Exception ex)
            {
                ultimoError =
                    ex.Message;
            }
        }

        throw new InvalidOperationException(
            ultimoError);
    }

    private static List<T> LeerItems<T>(
        string json)
    {
        json =
            LimpiarJson(json);

        using JsonDocument doc =
            JsonDocument.Parse(json);

        JsonElement root =
            doc.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (TryGetPropertyAny(root, out JsonElement ok, "ok") &&
                ok.ValueKind == JsonValueKind.False)
            {
                throw new InvalidOperationException(
                    ExtraerError(json));
            }

            if (TryGetPropertyAny(root, out JsonElement items, "items", "apps", "applications", "aplicaciones"))
                root = items;
        }

        if (root.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("La PC respondió, pero la lista no tiene formato válido.");

        return JsonSerializer.Deserialize<List<T>>(
                   root.GetRawText(),
                   JsonOptions) ??
               new List<T>();
    }

    private static bool TryGetPropertyAny(
        JsonElement root,
        out JsonElement value,
        params string[] names)
    {
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (names.Any(name => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string ExtraerError(
        string body)
    {
        body =
            LimpiarJson(body);

        if (string.IsNullOrWhiteSpace(body))
            return "La PC no respondió con detalle.";

        try
        {
            using JsonDocument doc =
                JsonDocument.Parse(body);

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (TryGetPropertyAny(doc.RootElement, out JsonElement error, "error", "message", "detail"))
                    return error.GetString() ?? body;
            }
        }
        catch
        {
        }

        return Recortar(body.Trim(), 500);
    }

    private static string LimpiarJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        return text
            .Replace("\0", "")
            .Trim('\uFEFF', '\u200B', ' ', '\r', '\n', '\t');
    }

    private static string Recortar(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Sin detalle.";

        text = text.Trim();
        return text.Length <= max ? text : text[..max];
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
