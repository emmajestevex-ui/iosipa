namespace RemoControlMobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        NavigationPage navigation =
            new NavigationPage(
                (AppConfig.BloqueoApp && LockSecurityService.IsPasswordConfigured)
                    ? new AppLockPage()
                    : new MainPage());

        navigation.BarBackgroundColor =
            Color.FromArgb("#101720");

        navigation.BarTextColor =
            Colors.White;

        return new Window(
            navigation);
    }
}