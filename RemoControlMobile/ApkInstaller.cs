namespace RemoControlMobile;

public static class ApkInstaller
{
    public static void Instalar(
        string rutaApk)
    {
#if ANDROID
        MainActivity.InstalarApk(
            rutaApk);
#endif
    }
}