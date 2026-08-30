namespace RemoControlMobile;

public static class AppConfig
{
    static AppConfig()
    {
        const string migrationKey = "config_general_v5";

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
            return Preferences.Default.Get(
                "servidor_activo",
                "");
        }

        set
        {
            Preferences.Default.Set(
                "servidor_activo",
                value?.Trim() ?? "");
        }
    }


    public static string Token
    {
        get
        {
            return Preferences.Default.Get(
                "token",
                "");
        }

        set
        {
            Preferences.Default.Set(
                "token",
                value?.Trim() ?? "");
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
        HttpClient cliente =
            new HttpClient();

        cliente.Timeout =
            TimeSpan.FromSeconds(
                timeoutSegundos);

        if (!string.IsNullOrWhiteSpace(Token))
        {
            cliente.DefaultRequestHeaders.Add(
                "X-Remo-Token",
                Token);
        }

        return cliente;
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
            using HttpClient cliente =
                CrearCliente(5);

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    servidor.TrimEnd('/') +
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

