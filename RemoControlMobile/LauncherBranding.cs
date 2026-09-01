namespace RemoControlMobile;

public static class LauncherBranding
{
    public static Task<bool> CrearAccesoPersonalizadoAsync(string nombre, string? logoPath, string? colorFondo = null)
    {
#if ANDROID
        try
        {
            var activity = MainActivity.Instancia;
            if (activity == null)
                return Task.FromResult(false);

            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.O)
                return Task.FromResult(false);

            var manager = (Android.Content.PM.ShortcutManager?)activity.GetSystemService(Android.Content.Context.ShortcutService);
            if (manager == null || !manager.IsRequestPinShortcutSupported)
                return Task.FromResult(false);

            string etiqueta = string.IsNullOrWhiteSpace(nombre) ? "RemoControl" : nombre.Trim();
            Android.Graphics.Drawables.Icon icon;

            if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
            {
                var logo = Android.Graphics.BitmapFactory.DecodeFile(logoPath);
                if (logo != null)
                {
                    const int size = 384;
                    var baseBitmap = Android.Graphics.Bitmap.CreateBitmap(size, size, Android.Graphics.Bitmap.Config.Argb8888!);
                    using var canvas = new Android.Graphics.Canvas(baseBitmap);
                    Android.Graphics.Color bg;
                    try { bg = Android.Graphics.Color.ParseColor(string.IsNullOrWhiteSpace(colorFondo) ? "#0B1119" : colorFondo); }
                    catch { bg = Android.Graphics.Color.Rgb(11, 17, 25); }
                    canvas.DrawColor(bg);

                    int margen = 56;
                    var destino = new Android.Graphics.Rect(margen, margen, size - margen, size - margen);
                    canvas.DrawBitmap(logo, null, destino, null);
                    icon = Android.Graphics.Drawables.Icon.CreateWithBitmap(baseBitmap);
                }
                else
                {
                    icon = Android.Graphics.Drawables.Icon.CreateWithResource(activity, activity.ApplicationInfo!.Icon);
                }
            }
            else
            {
                icon = Android.Graphics.Drawables.Icon.CreateWithResource(activity, activity.ApplicationInfo!.Icon);
            }

            var intent = new Android.Content.Intent(activity, typeof(MainActivity));
            intent.SetAction(Android.Content.Intent.ActionMain);
            intent.AddCategory(Android.Content.Intent.CategoryLauncher);

            var shortcut = new Android.Content.PM.ShortcutInfo.Builder(activity, "remocontrol_personalizado")
                .SetShortLabel(etiqueta)
                .SetLongLabel(etiqueta)
                .SetIcon(icon)
                .SetIntent(intent)
                .Build();

            manager.RequestPinShortcut(shortcut, null);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
#else
        return Task.FromResult(false);
#endif
    }
}
