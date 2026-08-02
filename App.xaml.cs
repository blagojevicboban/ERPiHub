using System.Windows;
using Velopack;

namespace ERPiHub;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        AppLog.Init();
        AppLog.RegistrujGlobalneHandlere(this);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Velopack inicijalizacija za prečice i proces instalacije/update-a
        VelopackApp.Build().Run();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLog.Zatvori();
        base.OnExit(e);
    }
}
