using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ErpHub.Models;
using ErpHub.Services;

namespace ErpHub;

public partial class MainWindow : Window
{
    private readonly ModuleDiscoveryService _discoveryService;
    private readonly ModuleLauncherService _launcherService;
    private readonly DispatcherTimer _statusTimer;

    public ObservableCollection<ModuleItem> Modules { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        _discoveryService = new ModuleDiscoveryService();
        _launcherService = new ModuleLauncherService();

        LoadData();

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        TxtVersionInfo.Text = $"Velopack Auto-Update Active • v{versionStr}";

        // Tajmer za automatsku proveru i osvežavanje statusa procesa na svakih 3 sekunde
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _statusTimer.Tick += (s, e) => RefreshModulesSilently();
        _statusTimer.Start();

        // Provera ažuriranja u pozadini pri pokretanju
        _ = CheckForUpdatesAsync();
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        try
        {
            var source = new Velopack.Sources.GithubSource(
                "https://github.com/blagojevicboban/ErpHub",
                null,
                false);
            var mgr = new Velopack.UpdateManager(source);
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion != null)
            {
                var dialog = new UpdateDialog(newVersion, mgr);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Greška pri proveri ažuriranja: {ex.Message}");
        }
    }

    private void LoadData()
    {
        RefreshModules();
    }

    private void RefreshModules()
    {
        Modules.Clear();
        var modList = _discoveryService.GetModules();
        foreach (var m in modList)
        {
            Modules.Add(m);
        }
        ModuleItemsControl.ItemsSource = Modules;
        TxtStatus.Text = $"Osveženi statusi modula i baza u {DateTime.Now:HH:mm:ss}";
    }

    private void RefreshModulesSilently()
    {
        foreach (var m in Modules)
        {
            _discoveryService.RefreshModuleStatus(m);
        }
    }

    private void BtnLaunchModule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ModuleItem module)
        {
            var companyToUse = module.SelectedCompany;

            TxtStatus.Text = companyToUse != null 
                ? $"Pokretanje modula '{module.Title}' za bazu '{companyToUse.Naziv}'..."
                : $"Pokretanje modula '{module.Title}'...";

            bool success = _launcherService.LaunchModule(module, companyToUse, (mod) =>
            {
                // Kada se proces modula zatvori, automatski osveži status na UI-u
                Dispatcher.Invoke(() =>
                {
                    _discoveryService.RefreshModuleStatus(mod);
                    TxtStatus.Text = $"Modul '{mod.Title}' je zatvoren u {DateTime.Now:HH:mm:ss}.";
                });
            });

            if (success)
            {
                TxtStatus.Text = $"Modul '{module.Title}' je uspešno pokrenut.";
                _discoveryService.RefreshModuleStatus(module);
            }
            else
            {
                TxtStatus.Text = $"Greška pri pokretanju modula '{module.Title}'.";
            }
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshModules();
    }

    private void BtnOpenBazeFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string folderPath && !string.IsNullOrWhiteSpace(folderPath) && System.IO.Directory.Exists(folderPath))
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", folderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri otvaranju foldera:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else
        {
            MessageBox.Show("Folder sa bazama za ovaj modul još nije kreiran ili nije pronađen na disku.", "Obaveštenje", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Podešavanja ERP Hub-a:\n\n- Putanje do modula i baza se automatski detektuju za svaki modul pojedinačno.\n- Izbor baze/preduzeća se nalazi direktno na kartici modula.\n- Velopack update je aktivan.",
            "ERP Hub Podešavanja",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}