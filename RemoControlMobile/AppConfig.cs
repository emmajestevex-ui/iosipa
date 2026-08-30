using System.Globalization;

namespace RemoControlMobile;

public static class AppConfig
{
    static AppConfig()
    {
        const string migrationKey = "config_general_v5_2";

        if (!Preferences.Default.Get(migrationKey, false))
        {
            // Una sola vez: elimina configuraciones antiguas de desarrollo
            // para que cada instalación use los datos de SU propia PC.
            Preferences.Default.Remove("servidor_activo");
            Preferences.Default.Remove("token");
            Preferences.Default.Remove("equipos_guardados");
            Preferences.Default.Remove("equipo_activo");
            Preferences.Default.Set("PrimeraConfiguracionCompletada", false);
            Preferences.Default.Set(migrationKey, true);
        }

        const string brandingKey = "branding_msi_center_v1";

        if (!Preferences.Default.Get(brandingKey, false))
        {
            string nombreActual =
                Preferences.Default.Get("brand_name", "");

            if (
                string.IsNullOrWhiteSpace(nombreActual) ||
                nombreActual.Equals("RemoControl", StringComparison.OrdinalIgnoreCase) ||
                nombreActual.Equals("Msi Control", StringComparison.OrdinalIgnoreCase))
            {
                Preferences.Default.Set("brand_name", "MSI Center");
            }

            string colorActual =
                Preferences.Default.Get("brand_background", "");

            if (
                string.IsNullOrWhiteSpace(colorActual) ||
                colorActual.Equals("#087BDB", StringComparison.OrdinalIgnoreCase) ||
                colorActual.Equals("#E31B2F", StringComparison.OrdinalIgnoreCase))
            {
                Preferences.Default.Set("brand_background", "#0B1119");
            }

            Preferences.Default.Set(brandingKey, true);
        }
    }

    public static string Servidor
    {
        get
        {
            string guardado =
                Preferences.Default.Get(
                "servidor_activo",
                "");

            string limpio =
                NormalizarServidor(
                    guardado);

            if (guardado != limpio)
            {
                Preferences.Default.Set(
                    "servidor_activo",
                    limpio);
            }

            return limpio;
        }

        set
        {
            Preferences.Default.Set(
                "servidor_activo",
                NormalizarServidor(value));
        }
    }


    public static string Token
    {
        get
        {
            string guardado =
                Preferences.Default.Get(
                "token",
                "");

            string limpio =
                LimpiarToken(
                    guardado);

            if (guardado != limpio)
            {
                Preferences.Default.Set(
                    "token",
                    limpio);
            }

            return limpio;
        }

        set
        {
            Preferences.Default.Set(
                "token",
                LimpiarToken(value));
        }
    }



    public static string NombrePersonalizado
    {
        get => Preferences.Default.Get("brand_name", "MSI Center");
        set => Preferences.Default.Set("brand_name", string.IsNullOrWhiteSpace(value) ? "MSI Center" : value.Trim());
    }

    public static string LogoPersonalizado
    {
        get => Preferences.Default.Get("brand_logo", "");
        set => Preferences.Default.Set("brand_logo", value ?? "");
    }

    public static bool BloqueoApp
    {
        get => Preferences.Default.Get("app_lock", false);
        set => Preferences.Default.Set("app_lock", value);
    }


    public static int DuracionHablarSegundos
    {
        get => Preferences.Default.Get("intercom_talk_seconds", 4);
        set => Preferences.Default.Set("intercom_talk_seconds", Math.Clamp(value, 2, 15));
    }

    public static int AudioLiveSegundos
    {
        get => Preferences.Default.Get("intercom_live_seconds", 1);
        set => Preferences.Default.Set("intercom_live_seconds", Math.Clamp(value, 1, 3));
    }

    public static string ColorFondo
    {
        get => Preferences.Default.Get("brand_background", "#0B1119");
        set => Preferences.Default.Set("brand_background", string.IsNullOrWhiteSpace(value) ? "#0B1119" : value.Trim());
    }

    public static int IntervaloPantalla
    {
        get
        {
            return Preferences.Default.Get(
                "intervalo_pantalla",
                350);
        }

        set
        {
            Preferences.Default.Set(
                "intervalo_pantalla",
                value);
        }
    }


    public static bool HayConfiguracion
    {
        get
        {
            return
                !string.IsNullOrWhiteSpace(Servidor) &&
                !string.IsNullOrWhiteSpace(Token);
        }
    }


    public static HttpClient CrearCliente(
        int timeoutSegundos = 10)
    {
        string servidor =
            Servidor;

        if (
            EsServidorTailscale(
                servidor) &&
            timeoutSegundos < 25)
        {
            timeoutSegundos =
                25;
        }

        HttpClient cliente =
            new HttpClient();

        cliente.Timeout =
            TimeSpan.FromSeconds(
                timeoutSegundos);

        AgregarTokenHeaders(
            cliente,
            Token);

        return cliente;
    }


    public static void AgregarTokenHeaders(
        HttpClient cliente,
        string? token)
    {
        token =
            LimpiarToken(
                token);

        if (string.IsNullOrWhiteSpace(
            token))
        {
            return;
        }

        cliente.DefaultRequestHeaders.Remove(
            "X-Remo-Token");

        cliente.DefaultRequestHeaders.Remove(
            "X-RemoControl-Token");

        cliente.DefaultRequestHeaders.Remove(
            "X-API-Key");

        cliente.DefaultRequestHeaders.Add(
            "X-Remo-Token",
            token);

        cliente.DefaultRequestHeaders.Add(
            "X-RemoControl-Token",
            token);

        cliente.DefaultRequestHeaders.Add(
            "X-API-Key",
            token);

        try
        {
            cliente.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }
        catch
        {
            // Si el token contiene un caracter no permitido para Bearer,
            // las cabeceras X-Remo-Token siguen funcionando.
        }
    }


    public static string Url(
        string endpoint)
    {
        string servidor =
            Servidor;

        if (string.IsNullOrWhiteSpace(
            servidor))
        {
            throw new InvalidOperationException(
                "Primero conecta una PC autorizada.");
        }

        if (string.IsNullOrWhiteSpace(
            endpoint))
        {
            endpoint =
                "/";
        }

        if (!endpoint.StartsWith(
            "/",
            StringComparison.Ordinal))
        {
            endpoint =
                "/" +
                endpoint;
        }

        return servidor.TrimEnd('/') +
            endpoint;
    }


    public static string UrlConToken(
        string endpoint,
        string? token = null)
    {
        string url =
            Url(
                endpoint);

        token =
            LimpiarToken(
                token ?? Token);

        if (string.IsNullOrWhiteSpace(
            token))
        {
            return url;
        }

        return AnexarTokenAUrl(
            url,
            token);
    }


    public static string AnexarTokenAUrl(
        string url,
        string? token)
    {
        token =
            LimpiarToken(
                token);

        if (string.IsNullOrWhiteSpace(
            token))
        {
            return url;
        }

        string separador =
            url.Contains(
                '?')
                ? "&"
                : "?";

        return url +
            separador +
            "token=" +
            Uri.EscapeDataString(
                token) +
            "&remoToken=" +
            Uri.EscapeDataString(
                token) +
            "&api_key=" +
            Uri.EscapeDataString(
                token);
    }


    public static async Task<HttpResponseMessage> GetAsyncConToken(
        HttpClient cliente,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage respuesta =
            await cliente.GetAsync(
                Url(
                    endpoint),
                cancellationToken);

        if (
            DebeReintentarConTokenEnUrl(
                respuesta))
        {
            respuesta.Dispose();

            respuesta =
                await cliente.GetAsync(
                    UrlConToken(
                        endpoint),
                    cancellationToken);
        }

        return respuesta;
    }


    public static async Task<HttpResponseMessage> PostAsyncConToken(
        HttpClient cliente,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage respuesta =
            await cliente.PostAsync(
                Url(
                    endpoint),
                null,
                cancellationToken);

        if (
            DebeReintentarConTokenEnUrl(
                respuesta))
        {
            respuesta.Dispose();

            respuesta =
                await cliente.PostAsync(
                    UrlConToken(
                        endpoint),
                    null,
                    cancellationToken);
        }

        return respuesta;
    }


    private static bool DebeReintentarConTokenEnUrl(
        HttpResponseMessage respuesta)
    {
        return
            !string.IsNullOrWhiteSpace(
                Token)
            &&
            (
                respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                respuesta.StatusCode == System.Net.HttpStatusCode.Forbidden
            );
    }


    public static bool EsServidorTailscale(
        string? servidor)
    {
        servidor =
            NormalizarServidor(
                servidor);

        return
            Uri.TryCreate(
                servidor,
                UriKind.Absolute,
                out Uri? uri)
            &&
            uri.Host.StartsWith(
                "100.",
                StringComparison.OrdinalIgnoreCase);
    }


    public static string NormalizarServidor(
        string? servidor)
    {
        servidor =
            LimpiarTextoPegado(
                servidor,
                true);

        if (string.IsNullOrWhiteSpace(
            servidor))
        {
            return "";
        }

        if (!servidor.StartsWith(
            "http://",
            StringComparison.OrdinalIgnoreCase)
            &&
            !servidor.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            servidor =
                "http://" +
                servidor;
        }

        if (!Uri.TryCreate(
            servidor,
            UriKind.Absolute,
            out Uri? uri))
        {
            return servidor.TrimEnd('/');
        }

        UriBuilder limpio =
            new UriBuilder(uri)
            {
                Path = "",
                Query = "",
                Fragment = ""
            };

        return limpio
            .Uri
            .ToString()
            .TrimEnd('/');
    }


    public static string LimpiarToken(
        string? token)
    {
        return LimpiarTextoPegado(
            token,
            true);
    }


    public static string LimpiarTextoPegado(
        string? texto,
        bool quitarEspaciosInternos)
    {
        if (string.IsNullOrWhiteSpace(
            texto))
        {
            return "";
        }

        System.Text.StringBuilder limpio =
            new System.Text.StringBuilder(
                texto.Length);

        foreach (char caracter in texto)
        {
            UnicodeCategory categoria =
                char.GetUnicodeCategory(
                    caracter);

            if (
                caracter == '\0' ||
                caracter == '\uFEFF' ||
                categoria == UnicodeCategory.Control ||
                categoria == UnicodeCategory.Format)
            {
                continue;
            }

            if (
                quitarEspaciosInternos &&
                char.IsWhiteSpace(
                    caracter))
            {
                continue;
            }

            limpio.Append(
                caracter);
        }

        return limpio
            .ToString()
            .Trim();
    }


    // ============================================================
    // PROBAR EL SERVIDOR GUARDADO POR ESTE USUARIO
    // ============================================================

    public static async Task<bool> DetectarServidor()
    {
        string servidor =
            Servidor?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(servidor))
        {
            return false;
        }

        return await ProbarServidor(
            servidor);
    }


    private static async Task<bool> ProbarServidor(
        string servidor)
    {
        try
        {
            servidor =
                NormalizarServidor(
                    servidor);

            if (string.IsNullOrWhiteSpace(
                servidor))
            {
                return false;
            }

            int timeout =
                Uri.TryCreate(
                    servidor,
                    UriKind.Absolute,
                    out Uri? uri)
                &&
                uri.Host.StartsWith(
                    "100.",
                    StringComparison.OrdinalIgnoreCase)
                    ? 25
                    : 8;

            using HttpClient cliente =
                CrearCliente(timeout);

            using HttpResponseMessage respuesta =
                await GetAsyncConToken(
                    cliente,
                    "/status");

            return respuesta.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }


    public static void Restaurar()
    {
        Servidor = "";
        Token = "";
        IntervaloPantalla = 350;
        Preferences.Default.Remove("equipos_guardados");

        Preferences.Default.Set(
            "PrimeraConfiguracionCompletada",
            false);
    }
}

