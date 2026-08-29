namespace RemoControlMobile;

public static class NotificationService
{
    public static async Task PedirPermiso()
    {
#if ANDROID
        try
        {
            if (
                OperatingSystem
                    .IsAndroidVersionAtLeast(
                        33))
            {
                await Permissions
                    .RequestAsync
                    <Permissions.PostNotifications>();
            }
        }
        catch
        {
        }
#endif
    }


    public static void Mostrar(
        string titulo,
        string mensaje)
    {
#if ANDROID
        try
        {
            MainActivity
                .MostrarNotificacion(
                    titulo,
                    mensaje);
        }
        catch
        {
        }
#endif
    }
}