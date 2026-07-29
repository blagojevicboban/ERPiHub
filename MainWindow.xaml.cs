using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ErpHub.Models;
using ErpHub.Services;

namespace ErpHub;

public partial class MainWindow : Window
{
    private readonly ModuleDiscoveryService _discoveryService;
    private readonly ModuleLauncherService _launcherService;
    private readonly CompanyService _companyService;

    public ObservableCollection<ModuleItem> Modules { get; } = new();
    public ObservableCollection<CompanyItem> Companies { get; } = new();

    public CompanyItem? ActiveCompany => CmbCompany.SelectedItem as CompanyItem;

    public MainWindow()
    {
        InitializeComponent();

        _discoveryService = new ModuleDiscoveryService();
        _launcherService = new ModuleLauncherService();
        _companyService = new CompanyService();

        LoadData();
    }

    private void LoadData()
    {
        // Učitavanje firmi
        Companies.Clear();
        var companyList = _companyService.GetCompanies();
        foreach (var c in companyList)
        {
            Companies.Add(c);
        }
        CmbCompany.ItemsSource = Companies;
        if (Companies.Count > 0)
        {
            CmbCompany.SelectedIndex = 0;
        }

        // Učitavanje modula
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
        TxtStatus.Text = $"Osveženi statusi modula u {DateTime.Now:HH:mm:ss}";
    }

    private void CmbCompany_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ActiveCompany != null)
        {
            TxtCompanyDb.Text = $"Baza: {ActiveCompany.DbPath}";
        }
        else
        {
            TxtCompanyDb.Text = "Baza: Nije izabrana";
        }
    }

    private void BtnLaunchModule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ModuleItem module)
        {
            if (ActiveCompany == null)
            {
                MessageBox.Show(
                    "Molimo izaberite aktivno preduzeće pre pokretanja modula.",
                    "Firma nije izabrana",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            TxtStatus.Text = $"Pokretanje modula '{module.Title}' za firmu '{ActiveCompany.Naziv}'...";
            bool success = _launcherService.LaunchModule(module, ActiveCompany);
            if (success)
            {
                TxtStatus.Text = $"Modul '{module.Title}' je uspešno pokrenut.";
                _discoveryService.RefreshModuleStatus(module);
                ModuleItemsControl.Items.Refresh();
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

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Podešavanja ERP Hub-a:\n\n- Putanje do modula se automatski detektuju.\n- Baze se konfigurišu u `companies.json`.\n- Velopack update je aktivan.",
            "ERP Hub Podešavanja",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}