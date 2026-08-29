using System.Net.Http.Json;

namespace RemoControlMobile;

public static class UpdateService
{
    private const string VersionUrl =
        "https://raw.githubusercontent.com/Genao0909/RemoControlMobile-Releases/main/version.json";


    public static async Task<UpdateInfo?>
        BuscarActualizacion()
    {
        try
        {
            using HttpClient cliente =
                new HttpClient();

            cliente.Timeout =
                TimeSpan.FromSeconds(
                    10);

            string url =
                VersionUrl +
                "?t=" +
                DateTimeOffset
                    .UtcNow
                    .ToUnixTimeSeconds();

            UpdateInfo? info =
                await cliente
                    .GetFromJsonAsync
                    <UpdateInfo>(
                        url);

            return info;
        }
        catch
        {
            return null;
        }
    }


    public static bool HayActualizacion(
        UpdateInfo info)
    {
        try
        {
            int versionActual;

            if (!int.TryParse(
                AppInfo.Current
                    .BuildString,
                out versionActual))
            {
                return false;
            }

            return
                info.versionCode >
                versionActual;
        }
        catch
        {
            return false;
        }
    }
}


public class UpdateInfo
{
    public int versionCode
    {
        get;
        set;
    }

    public string? versionName
    {
        get;
        set;
    }

    public string? apkUrl
    {
        get;
        set;
    }

    public string? notes
    {
        get;
        set;
    }
}