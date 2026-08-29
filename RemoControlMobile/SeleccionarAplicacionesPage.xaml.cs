using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
                    string url =
                        AppConfig.Servidor +
                        "/apps/add?id=" +
                        Uri.EscapeDataString(
                            app.id ??
                            "");

                    using HttpResponseMessage respuesta =
                        await cliente.PostAsync(
                            url,
                            null);

                    if (respuesta.IsSuccessStatusCode)
                    {
                        agregadas++;
                    }
                    else
                    {
                        errores.Add(
                            app.name ??
                            "Aplicación");
                    }
                }
                catch
                {
                    errores.Add(
                        app.name ??
                        "Aplicación");
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
                errores.Count,
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
}
