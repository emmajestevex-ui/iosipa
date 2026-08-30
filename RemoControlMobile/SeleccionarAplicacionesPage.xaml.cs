using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace RemoControlMobile;

public partial class SeleccionarAplicacionesPage : ContentPage
{
    private readonly List<AppDisponible> todas;

    private readonly Func<Task> alFinalizar;

    public SeleccionarAplicacionesPage(
        List<AppDisponible> aplicaciones,
        Func<Task> alFinalizar)
    {
        InitializeComponent();
        Shell.SetNavBarIsVisible(this, false);

        this.todas =
            aplicaciones ??
            new List<AppDisponible>();

        this.alFinalizar =
            alFinalizar;

        foreach (AppDisponible app in todas)
        {
            app.selected =
                false;
        }

        MostrarLista(
            todas);

        ActualizarContador();
    }

    private string U(string path)
    {
        return AppConfig.Url(
            path);
    }

    // ============================================================
    // LISTA
    // ============================================================

    private void MostrarLista(
        IEnumerable<AppDisponible> lista)
    {
        listaDisponibles.ItemsSource =
            null;

        listaDisponibles.ItemsSource =
            lista.ToList();
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
                todas);

            return;
        }

        List<AppDisponible> filtradas =
            todas
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

    // ============================================================
    // SELECCIÓN
    // ============================================================

    private void CheckApp_CheckedChanged(
        object sender,
        CheckedChangedEventArgs e)
    {
        if (
            sender is not CheckBox check)
        {
            return;
        }

        if (
            check.BindingContext
            is not AppDisponible app)
        {
            return;
        }

        app.selected =
            e.Value;

        ActualizarContador();
    }

    private void ActualizarContador()
    {
        int cantidad =
            todas.Count(
                x => x.selected);

        lblSeleccionadas.Text =
            cantidad == 1
                ? "1 seleccionada"
                : cantidad +
                  " seleccionadas";

        btnAgregar.IsEnabled =
            cantidad > 0;

        btnAgregar.Opacity =
            cantidad > 0
                ? 1
                : 0.55;
    }

    // ============================================================
    // AGREGAR
    // ============================================================

    private async void BtnAgregar_Clicked(
        object sender,
        EventArgs e)
    {
        List<AppDisponible> seleccionadas =
            todas
                .Where(
                    x => x.selected)
                .ToList();

        if (seleccionadas.Count == 0)
        {
            return;
        }

        btnAgregar.IsEnabled =
            false;

        btnAgregar.Text =
            "Agregando...";

        int agregadas =
            0;

        List<string> errores =
            new();

        try
        {
            using HttpClient cliente =
                AppConfig.CrearCliente(
                    25);

            foreach (AppDisponible app in seleccionadas)
            {
                try
                {
                    string id =
                        Uri.EscapeDataString(
                            app.id ?? "");

                    await PostFirstAsync(
                        cliente,
                        "/apps/add?id=" + id,
                        "/applications/add?id=" + id,
                        "/pro/apps/add?id=" + id);

                    agregadas++;
                }
                catch (Exception ex)
                {
                    errores.Add(
                        (app.name ?? "Aplicación") +
                        ": " +
                        Recortar(ex.Message, 120));
                }
            }

            if (
                agregadas > 0 &&
                alFinalizar != null)
            {
                await alFinalizar();
            }

            if (errores.Count == 0)
            {
                await DisplayAlertAsync(
                    "Aplicaciones",
                    agregadas == 1
                        ? "Aplicación agregada correctamente."
                        : agregadas +
                          " aplicaciones agregadas correctamente.",
                    "Aceptar");

                await Navigation.PopAsync();

                return;
            }

            await DisplayAlertAsync(
                "Resultado",
                "Agregadas: " +
                agregadas +
                "\nNo se pudieron agregar: " +
                errores.Count +
                (
                    errores.Count == 0
                        ? ""
                        : "\n\n" + string.Join("\n", errores.Take(4))
                ),
                "Aceptar");

            if (agregadas > 0)
            {
                await Navigation.PopAsync();
            }
        }
        finally
        {
            btnAgregar.Text =
                "Agregar";

            btnAgregar.IsEnabled =
                true;

            ActualizarContador();
        }
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
                    LimpiarTexto(
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

    private static string ExtraerError(string body)
    {
        body =
            LimpiarTexto(body);

        if (string.IsNullOrWhiteSpace(body))
            return "La PC no respondió con detalle.";

        try
        {
            using JsonDocument doc =
                JsonDocument.Parse(body);

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in doc.RootElement.EnumerateObject())
                {
                    if (property.Name.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("message", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("detail", StringComparison.OrdinalIgnoreCase))
                    {
                        return property.Value.GetString() ?? body;
                    }
                }
            }
        }
        catch
        {
        }

        return Recortar(body.Trim(), 500);
    }

    private static string LimpiarTexto(string text)
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
}
