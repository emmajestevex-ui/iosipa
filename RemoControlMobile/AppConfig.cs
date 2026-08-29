namespace RemoControlMobile;

public static class AppConfig
{
    private const string ServidorLocal =
        "http://192.168.0.159:5050";

    private const string ServidorTailscale =
        "http://100.82.40.107:5050";

    private const string TokenDefault =
        "REMO-123456-CAMBIA-ESTA-CLAVE";


    public static string Servidor
    {
        get
        {
            return Preferences.Default.Get(
                "servidor_activo",
                ServidorTailscale);
        }

        set
        {
            Preferences.Default.Set(
                "servidor_activo",
                value);
        }
    }


    public static string Token
    {
        get
        {
            return Preferences.Default.Get(
                "token",
                TokenDefault);
        }

        set
        {
            Preferences.Default.Set(
                "token",
                value);
        }
    }


    public static int IntervaloPantalla
    {
        get
        {
            return Preferences.Default.Get(
                "intervalo_pantalla",
                700);
        }

        set
        {
            Preferences.Default.Set(
                "intervalo_pantalla",
                value);
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

        cliente.DefaultRequestHeaders.Add(
            "X-Remo-Token",
            Token);

        return cliente;
    }


    public static async Task<bool> DetectarServidor()
    {
        // Primero intentamos la red local
        if (await ProbarServidor(
            ServidorLocal))
        {
            Servidor =
                ServidorLocal;

            return true;
        }

        // Si no responde, intentamos Tailscale
        if (await ProbarServidor(
            ServidorTailscale))
        {
            Servidor =
                ServidorTailscale;

            return true;
        }

        return false;
    }


    private static async Task<bool> ProbarServidor(
        string servidor)
    {
        try
        {
            using HttpClient cliente =
                CrearCliente(3);

            using HttpResponseMessage respuesta =
                await cliente.GetAsync(
                    servidor +
                    "/info");

            return respuesta.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }


    public static void Restaurar()
    {
        Servidor =
            ServidorTailscale;

        Token =
            TokenDefault;

        IntervaloPantalla =
            700;
    }
}