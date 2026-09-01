namespace RemoControlMobile;

public partial class AppLockPage : ContentPage
{
    private bool _authenticating;

    public AppLockPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        btnBiometric.IsVisible = LockSecurityService.BiometricsEnabled && await DeviceBiometricAuth.IsAvailableAsync();

        if (btnBiometric.IsVisible)
            await TryBiometricAsync();
        else
            txtPassword.Focus();
    }

    private async void BtnUnlock_Clicked(object sender, EventArgs e) => await TryPasswordAsync();
    private async void TxtPassword_Completed(object sender, EventArgs e) => await TryPasswordAsync();
    private async void BtnBiometric_Clicked(object sender, EventArgs e) => await TryBiometricAsync();

    private async Task TryPasswordAsync()
    {
        lblError.Text = "";
        if (await LockSecurityService.VerifyPasswordAsync(txtPassword.Text ?? string.Empty))
        {
            await OpenAppAsync();
            return;
        }

        lblError.Text = "Contraseña incorrecta.";
        txtPassword.Text = "";
        txtPassword.Focus();
    }

    private async Task TryBiometricAsync()
    {
        if (_authenticating)
            return;

        _authenticating = true;
        lblError.Text = "";

        try
        {
            if (await DeviceBiometricAuth.AuthenticateAsync())
                await OpenAppAsync();
            else
                lblError.Text = "No se pudo validar la biometría. Usa tu contraseña.";
        }
        finally
        {
            _authenticating = false;
        }
    }

    private async Task OpenAppAsync()
    {
        await Navigation.PushAsync(new MainPage());
        Navigation.RemovePage(this);
    }
}
