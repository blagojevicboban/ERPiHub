using System.Windows;
using Velopack;

namespace ErpHub;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Velopack inicijalizacija za prečice i proces instalacije/update-a
        VelopackApp.Build().Run();

        base.OnStartup(e);
    }
}
