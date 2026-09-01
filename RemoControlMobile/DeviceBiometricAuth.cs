namespace RemoControlMobile;

public static class DeviceBiometricAuth
{
    public static Task<bool> IsAvailableAsync()
    {
#if ANDROID
        return Task.FromResult(Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P);
#elif IOS
        using var context = new LocalAuthentication.LAContext();
        bool ok = context.CanEvaluatePolicy(LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _);
        return Task.FromResult(ok);
#else
        return Task.FromResult(false);
#endif
    }

    public static async Task<bool> AuthenticateAsync()
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.P)
            return false;

        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is null)
            return false;

        var tcs = new TaskCompletionSource<bool>();
        var callback = new AndroidBiometricCallback(tcs);
        var cancel = new Android.OS.CancellationSignal();

        var builder = new Android.Hardware.Biometrics.BiometricPrompt.Builder(activity)
            .SetTitle("Desbloquear RemoControl")
            .SetSubtitle("Usa tu huella o biometría")
            .SetDescription("Confirma tu identidad para abrir la app")
            .SetNegativeButton(
                "Usar contraseña",
                activity.MainExecutor,
                new AndroidNegativeClickListener(tcs));

        var prompt = builder.Build();
        prompt.Authenticate(cancel, activity.MainExecutor, callback);
        return await tcs.Task;
#elif IOS
        using var context = new LocalAuthentication.LAContext
        {
            LocalizedFallbackTitle = "Usar contraseña"
        };

        if (!context.CanEvaluatePolicy(LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _))
            return false;

        try
        {
            var result = await context.EvaluatePolicyAsync(
                LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics,
                "Desbloquea RemoControl");
            return result.Item1;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

#if ANDROID
    private sealed class AndroidBiometricCallback : Android.Hardware.Biometrics.BiometricPrompt.AuthenticationCallback
    {
        private readonly TaskCompletionSource<bool> _tcs;
        public AndroidBiometricCallback(TaskCompletionSource<bool> tcs) => _tcs = tcs;

        public override void OnAuthenticationSucceeded(Android.Hardware.Biometrics.BiometricPrompt.AuthenticationResult? result)
        {
            base.OnAuthenticationSucceeded(result);
            _tcs.TrySetResult(true);
        }

        public override void OnAuthenticationError(Android.Hardware.Biometrics.BiometricErrorCode errorCode, Java.Lang.ICharSequence? errString)
        {
            base.OnAuthenticationError(errorCode, errString);
            _tcs.TrySetResult(false);
        }

        public override void OnAuthenticationFailed()
        {
            base.OnAuthenticationFailed();
        }
    }

    private sealed class AndroidNegativeClickListener : Java.Lang.Object, Android.Content.IDialogInterfaceOnClickListener
    {
        private readonly TaskCompletionSource<bool> _tcs;
        public AndroidNegativeClickListener(TaskCompletionSource<bool> tcs) => _tcs = tcs;

        public void OnClick(Android.Content.IDialogInterface? dialog, int which)
        {
            _tcs.TrySetResult(false);
        }
    }
#endif
}
