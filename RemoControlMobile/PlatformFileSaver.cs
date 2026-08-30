namespace RemoControlMobile;

public static class PlatformFileSaver
{
    public static async Task<string> GuardarEnDescargasAsync(string rutaTemporal, string nombre, CancellationToken token = default)
    {
#if ANDROID
        var activity = MainActivity.Instancia ?? throw new InvalidOperationException("Android no está listo.");
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
        {
            var values = new Android.Content.ContentValues();
            values.Put("_display_name", nombre);
            values.Put("mime_type", ObtenerMime(nombre));
            values.Put("relative_path", Android.OS.Environment.DirectoryDownloads + "/RemoControl");

            var uri = activity.ContentResolver.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri, values)
                      ?? throw new IOException("No se pudo crear el archivo en Descargas.");

            await using Stream origen = File.OpenRead(rutaTemporal);
            await using Stream destino = activity.ContentResolver.OpenOutputStream(uri)
                                        ?? throw new IOException("No se pudo abrir Descargas.");
            await origen.CopyToAsync(destino, token);
            return "Descargas/RemoControl/" + nombre;
        }
#endif
        string carpeta = Path.Combine(FileSystem.AppDataDirectory, "Downloads");
        Directory.CreateDirectory(carpeta);
        string destinoLocal = Path.Combine(carpeta, nombre);
        await using (Stream origen = File.OpenRead(rutaTemporal))
        await using (Stream destino = File.Create(destinoLocal))
            await origen.CopyToAsync(destino, token);
        return destinoLocal;
    }

    private static string ObtenerMime(string nombre)
    {
        string ext = Path.GetExtension(nombre).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
