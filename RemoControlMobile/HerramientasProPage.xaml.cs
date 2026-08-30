using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RemoControlMobile;

public partial class HerramientasProPage : ContentPage
{
    private readonly HttpClient http = AppConfig.CrearCliente(15);

    public HerramientasProPage()
    {
        InitializeComponent();
    }

    private string U(string p) => AppConfig.Servidor.TrimEnd('/') + p;

    private async Task<bool> Call(string p)
    {
        try
        {
            using var r = await http.PostAsync(U(p), null);
            string t = await r.Content.ReadAsStringAsync();
            if (!r.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("MSI Center", ExtraerError(t), "Aceptar");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Sin conexión", ex.Message, "Aceptar");
            return false;
        }
    }

    private async void BtnContinuar_Clicked(object s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtUrl.Text)) return;
        await Call("/pro/continue?url=" + Uri.EscapeDataString(txtUrl.Text));
    }

    private async void BtnApagar_Clicked(object s, EventArgs e) => await Schedule("shutdown", "apagar");
    private async void BtnSuspender_Clicked(object s, EventArgs e) => await Schedule("sleep", "suspender");

    private async void BtnApagarAhora_Clicked(object s, EventArgs e)
    {
        if (!await DisplayAlertAsync("Apagar laptop", "¿Apagar la laptop ahora?", "Apagar", "Cancelar"))
            return;

        await Power("shutdown", "apagado");
    }

    private async void BtnSuspenderAhora_Clicked(object s, EventArgs e)
    {
        if (!await DisplayAlertAsync("Suspender laptop", "¿Suspender la laptop ahora?", "Suspender", "Cancelar"))
            return;

        await Power("sleep", "suspendida");
    }

    private async void BtnBloquear_Clicked(object s, EventArgs e)
    {
        if (!await DisplayAlertAsync("Bloquear laptop", "¿Bloquear la laptop ahora?", "Bloquear", "Cancelar"))
            return;

        await Power("lock", "bloqueada");
    }

    private async void BtnDesbloquear_Clicked(object s, EventArgs e)
    {
        bool continuar = await DisplayAlertAsync(
            "Desbloquear laptop",
            "Por seguridad no se guarda ni se manda tu contraseña. Puedo pedirle a la PC que despierte o muestre la pantalla de inicio de sesión; el PIN, contraseña o Windows Hello se usa en la laptop.",
            "Preparar",
            "Cancelar");

        if (!continuar)
            return;

        await Power("wake", "lista para iniciar sesión");
    }

    private async Task Power(string accion, string estado)
    {
        lblEstado.Text = "Enviando orden a la PC...";

        string[] rutas = accion switch
        {
            "shutdown" => new[]
            {
                "/pro/power?action=shutdown",
                "/pro/shutdown",
                "/pro/schedule?action=shutdown&minutes=0"
            },
            "sleep" => new[]
            {
                "/pro/power?action=sleep",
                "/pro/sleep",
                "/pro/schedule?action=sleep&minutes=0"
            },
            "lock" => new[]
            {
                "/pro/power?action=lock",
                "/pro/lock"
            },
            "wake" => new[]
            {
                "/pro/power?action=wake",
                "/pro/wake",
                "/pro/unlock-request"
            },
            _ => new[]
            {
                "/pro/power?action=" + Uri.EscapeDataString(accion)
            }
        };

        if (await CallFirst(rutas))
            lblEstado.Text = "Laptop " + estado + ".";
    }

    private async Task<bool> CallFirst(params string[] rutas)
    {
        string ultimoError = "";

        foreach (string ruta in rutas)
        {
            try
            {
                using HttpResponseMessage response = await http.PostAsync(U(ruta), null);
                string body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return true;

                ultimoError = ExtraerError(body);
            }
            catch (Exception ex)
            {
                ultimoError = ex.Message;
            }
        }

        lblEstado.Text = "No se pudo completar la orden.";
        await DisplayAlertAsync("MSI Center", ultimoError, "Aceptar");
        return false;
    }

    private async Task Schedule(string accion, string nombre)
    {
        if (!int.TryParse(txtMinutos.Text, out int m) || m < 1)
        {
            await DisplayAlertAsync("Tiempo", "Escribe los minutos.", "Aceptar");
            return;
        }
        if (await DisplayAlertAsync("Confirmar", $"¿Programar {nombre} la PC en {m} minutos?", "Sí", "No"))
            await Call($"/pro/schedule?action={accion}&minutes={m}");
    }

    private async void BtnPrev_Clicked(object s, EventArgs e) => await Call("/pro/presentation?action=prev");
    private async void BtnNext_Clicked(object s, EventArgs e) => await Call("/pro/presentation?action=next");
    private async void BtnStart_Clicked(object s, EventArgs e) => await Call("/pro/presentation?action=start");
    private async void BtnExit_Clicked(object s, EventArgs e) => await Call("/pro/presentation?action=exit");

    private async void BtnRing_Clicked(object s, EventArgs e)
    {
        string mensaje = string.IsNullOrWhiteSpace(txtMensaje.Text) ? "MSI Center" : txtMensaje.Text.Trim();
        lblEstado.Text = "Enviando timbre a la PC...";

        if (await RingPcAsync(mensaje))
        {
            lblEstado.Text = "Timbre enviado a la PC.";
            try { await TextToSpeech.Default.SpeakAsync(mensaje); } catch { }
        }
    }

    private async Task<bool> RingPcAsync(string mensaje)
    {
        string encoded = Uri.EscapeDataString(mensaje);
        string path = "/pro/ring?message=" + encoded + "&sound=1&speak=1";
        var payload = new
        {
            message = mensaje,
            mensaje,
            playSound = true,
            sound = true,
            speak = true,
            readMessage = true
        };

        try
        {
            using JsonContent content = JsonContent.Create(payload);
            using HttpResponseMessage response = await http.PostAsync(U(path), content);
            string body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return true;

            if (response.StatusCode is HttpStatusCode.BadRequest or
                HttpStatusCode.UnsupportedMediaType or
                HttpStatusCode.MethodNotAllowed)
            {
                using HttpResponseMessage fallback =
                    await http.PostAsync(U("/pro/ring?message=" + encoded), null);
                string fallbackBody = await fallback.Content.ReadAsStringAsync();
                if (fallback.IsSuccessStatusCode)
                    return true;

                body = string.IsNullOrWhiteSpace(fallbackBody) ? body : fallbackBody;
            }

            lblEstado.Text = "No se pudo enviar el timbre.";
            await DisplayAlertAsync("Timbre", ExtraerError(body), "Aceptar");
            return false;
        }
        catch (Exception ex)
        {
            lblEstado.Text = "Sin conexión con la PC.";
            await DisplayAlertAsync("Sin conexión", ex.Message, "Aceptar");
            return false;
        }
    }

    private async Task<string> Get(string p)
    {
        try
        {
            return await http.GetStringAsync(U(p));
        }
        catch (Exception ex)
        {
            lblEstado.Text = ex.Message;
            return "";
        }
    }

    private async void BtnRecientes_Clicked(object s, EventArgs e)
    {
        listaRecientes.Children.Clear();
        string json = await Get("/pro/recent");
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                string name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "Archivo" : "Archivo";
                string path = item.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                string modified = item.TryGetProperty("modified", out var m) ? m.GetString() ?? "" : "";
                listaRecientes.Children.Add(CrearFila(name, string.IsNullOrWhiteSpace(modified) ? path : modified));
            }
        }
        catch { listaRecientes.Children.Add(CrearFila("Sin resultados", "No se pudieron interpretar los archivos recientes.")); }
    }

    private async void BtnWifi_Clicked(object s, EventArgs e)
    {
        listaWifi.Children.Clear();
        string json = await Get("/pro/wifi");
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            string text = doc.RootElement.GetProperty("text").GetString() ?? "";
            var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in Regex.Matches(text, @"(?:All User Profile|Perfil de todos los usuarios|Perfil de usuario)\s*:\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                string nombre = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(nombre)) nombres.Add(nombre);
            }
            if (nombres.Count == 0)
            {
                listaWifi.Children.Add(CrearFila("Sin perfiles detectados", "Windows no devolvió redes Wi‑Fi guardadas."));
                return;
            }
            foreach (string nombre in nombres)
            {
                Button b = new Button { Text = nombre, BackgroundColor = Color.FromArgb("#182535"), TextColor = Colors.White, CornerRadius = 10, HorizontalOptions = LayoutOptions.Fill };
                b.Clicked += async (_, __) => await Call("/pro/wifi?profile=" + Uri.EscapeDataString(nombre));
                listaWifi.Children.Add(b);
            }
        }
        catch { listaWifi.Children.Add(CrearFila("Error", "No se pudo leer la lista de Wi‑Fi.")); }
    }

    private async void BtnPrinters_Clicked(object s, EventArgs e)
    {
        listaImpresoras.Children.Clear();
        string json = await Get("/pro/printers");
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                string name = item.GetProperty("name").GetString() ?? "Impresora";
                bool offline = item.TryGetProperty("offline", out var off) && off.GetBoolean();
                bool def = item.TryGetProperty("default", out var d) && d.GetBoolean();
                Button b = new Button
                {
                    Text = name + "\n" + (offline ? "Fuera de línea" : "Disponible") + (def ? " • Predeterminada" : ""),
                    BackgroundColor = Color.FromArgb("#182535"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    HorizontalOptions = LayoutOptions.Fill
                };
                b.Clicked += async (_, __) =>
                {
                    string? accion = await DisplayActionSheetAsync(name, "Cancelar", null, "Pausar", "Reanudar", "Cancelar todos los trabajos");
                    string? api = accion switch
                    {
                        "Pausar" => "pause",
                        "Reanudar" => "resume",
                        "Cancelar todos los trabajos" => "cancel",
                        _ => null
                    };
                    if (api != null)
                        await Call("/pro/printers?action=" + api + "&printer=" + Uri.EscapeDataString(name));
                };
                listaImpresoras.Children.Add(b);
            }
        }
        catch { listaImpresoras.Children.Add(CrearFila("Error", "No se pudo leer la lista de impresoras.")); }
    }

    private async void BtnBluetooth_Clicked(object s, EventArgs e)
    {
        listaBluetooth.Children.Clear();
        string json = await Get("/pro/bluetooth");
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                string name = item.GetProperty("name").GetString() ?? "Bluetooth";
                string status = item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";
                Button b = new Button
                {
                    Text = name + "\n" + (string.IsNullOrWhiteSpace(status) ? "Dispositivo conocido" : status),
                    BackgroundColor = Color.FromArgb("#182535"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    HorizontalOptions = LayoutOptions.Fill
                };
                b.Clicked += async (_, __) =>
                {
                    string? accion = await DisplayActionSheetAsync(name, "Cancelar", null, "Habilitar", "Deshabilitar", "Abrir Bluetooth en la PC");
                    if (accion == "Abrir Bluetooth en la PC")
                        await Call("/pro/bluetooth?action=open");
                    else if (accion == "Habilitar")
                        await Call("/pro/bluetooth?action=enable&name=" + Uri.EscapeDataString(name));
                    else if (accion == "Deshabilitar")
                        await Call("/pro/bluetooth?action=disable&name=" + Uri.EscapeDataString(name));
                };
                listaBluetooth.Children.Add(b);
            }
        }
        catch { listaBluetooth.Children.Add(CrearFila("Error", "No se pudo leer Bluetooth.")); }
    }

    private async void BtnAbrirImpresoras_Clicked(object s, EventArgs e) => await Call("/pro/printers?action=open");
    private async void BtnAbrirBluetooth_Clicked(object s, EventArgs e) => await Call("/pro/bluetooth?action=open");

    private static Border CrearFila(string titulo, string detalle)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#0D1621"),
            Stroke = Color.FromArgb("#223247"),
            Padding = 12,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = titulo, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 13 },
                    new Label { Text = detalle, TextColor = Color.FromArgb("#8191A5"), FontSize = 10, LineBreakMode = LineBreakMode.TailTruncation }
                }
            }
        };
    }

    private static string ExtraerError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "La PC no respondió con detalle.";

        try
        {
            using JsonDocument doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var e)) return e.GetString() ?? body;
        }
        catch { }
        return body;
    }

    private async void BtnVolver_Clicked(object sender, EventArgs e) => await Navigation.PopAsync();
}
