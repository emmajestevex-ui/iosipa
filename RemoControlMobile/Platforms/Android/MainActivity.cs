using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace RemoControlMobile
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.Orientation |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize |
            ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public static MainActivity? Instancia
        {
            get;
            private set;
        }


        private const string CanalId =
            "remocontrol_general";


        // =========================================================
        // ON CREATE
        // =========================================================

        protected override void OnCreate(
            Bundle? savedInstanceState)
        {
            base.OnCreate(
                savedInstanceState);

            Instancia =
                this;

            CrearCanalNotificaciones();
        }


        // =========================================================
        // ORIENTACIÓN
        // =========================================================

        public void PonerHorizontal()
        {
            RequestedOrientation =
                ScreenOrientation.SensorLandscape;
        }


        public void PonerVertical()
        {
            RequestedOrientation =
                ScreenOrientation.SensorPortrait;
        }


        // =========================================================
        // NOTIFICACIONES
        // =========================================================

        private void CrearCanalNotificaciones()
        {
            if (
                Build.VERSION.SdkInt <
                BuildVersionCodes.O)
            {
                return;
            }

            NotificationChannel canal =
                new NotificationChannel(
                    CanalId,
                    "RemoControl",
                    NotificationImportance.Default);

            NotificationManager? gestor =
                GetSystemService(
                    Android.Content.Context
                        .NotificationService)
                as NotificationManager;

            if (gestor == null)
            {
                return;
            }

            gestor.CreateNotificationChannel(
                canal);
        }


        public static void MostrarNotificacion(
            string titulo,
            string mensaje)
        {
            MainActivity? actividad =
                Instancia;

            if (actividad == null)
            {
                return;
            }

            NotificationCompat.Builder builder =
                new NotificationCompat.Builder(
                    actividad,
                    CanalId);

            builder
                .SetSmallIcon(
                    Resource.Mipmap.appicon)
                .SetContentTitle(
                    titulo)
                .SetContentText(
                    mensaje)
                .SetAutoCancel(
                    true)
                .SetPriority(
                    NotificationCompat
                        .PriorityDefault);

            NotificationManagerCompat gestor =
                NotificationManagerCompat
                    .From(
                        actividad);

            gestor.Notify(
                System.Environment
                    .TickCount,
                builder.Build());
        }


        // =========================================================
        // INSTALAR APK
        // =========================================================

        public static void InstalarApk(
            string rutaApk)
        {
            MainActivity? actividad =
                Instancia;

            if (actividad == null)
            {
                return;
            }

            try
            {
                Java.IO.File archivo =
                    new Java.IO.File(
                        rutaApk);

                Android.Net.Uri uri =
                    AndroidX.Core.Content.FileProvider.GetUriForFile(
                        actividad,
                        actividad.PackageName +
                        ".fileprovider",
                        archivo);

                Intent intent =
                    new Intent(
                        Intent.ActionView);

                intent.SetDataAndType(
                    uri,
                    "application/vnd.android.package-archive");

                intent.AddFlags(
                    ActivityFlags
                        .GrantReadUriPermission);

                intent.AddFlags(
                    ActivityFlags
                        .NewTask);

                actividad.StartActivity(
                    intent);
            }
            catch (Exception ex)
            {
                Android.Widget.Toast
                    .MakeText(
                        actividad,
                        "No se pudo abrir el instalador: " +
                        ex.Message,
                        Android.Widget
                            .ToastLength
                            .Long)
                    ?.Show();
            }
        }
    }
}