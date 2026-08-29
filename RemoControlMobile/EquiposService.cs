using System.Text.Json;

namespace RemoControlMobile;

public static class EquiposService
{
    private const string Clave =
        "equipos_guardados";


    public static List<EquipoConfig> Obtener()
    {
        try
        {
            string json =
                Preferences.Default.Get(
                    Clave,
                    "[]");

            return
                JsonSerializer.Deserialize
                <List<EquipoConfig>>(
                    json) ??
                new List<EquipoConfig>();
        }
        catch
        {
            return
                new List<EquipoConfig>();
        }
    }


    public static void Guardar(
        List<EquipoConfig> equipos)
    {
        string json =
            JsonSerializer.Serialize(
                equipos);

        Preferences.Default.Set(
            Clave,
            json);
    }


    public static void Activar(
        EquipoConfig equipo)
    {
        AppConfig.Servidor =
            equipo.Servidor;

        AppConfig.Token =
            equipo.Token;
    }
}