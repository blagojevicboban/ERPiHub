using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ErpHub.Models;

public enum ModuleStatus
{
    NotInstalled,
    Installed,
    Running,
    UpdateAvailable
}

public class ModuleItem : INotifyPropertyChanged
{
    private ModuleStatus _status = ModuleStatus.NotInstalled;
    private string _exePath = string.Empty;
    private string _installedVersion = "1.0.0";
    private string _availableVersion = string.Empty;
    private DateTime? _lastLaunched;
    private string _companyCountText = "📁 0 baza firmi";
    private string _bazeFolderPath = string.Empty;
    private CompanyItem? _selectedCompany;

    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Subtitle { get; set; }
    public required string Description { get; set; }
    public required string Icon { get; set; }
    public required string HeaderGradientStart { get; set; }
    public required string HeaderGradientEnd { get; set; }

    public ObservableCollection<CompanyItem> Companies { get; } = new();

    public CompanyItem? SelectedCompany
    {
        get => _selectedCompany;
        set { if (_selectedCompany != value) { _selectedCompany = value; OnPropertyChanged(); } }
    }

    public string ExePath
    {
        get => _exePath;
        set { if (_exePath != value) { _exePath = value; OnPropertyChanged(); } }
    }

    public string InstalledVersion
    {
        get => _installedVersion;
        set { if (_installedVersion != value) { _installedVersion = value; OnPropertyChanged(); } }
    }

    public string AvailableVersion
    {
        get => _availableVersion;
        set { if (_availableVersion != value) { _availableVersion = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); } }
    }

    public ModuleStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(CanLaunch));
                OnPropertyChanged(nameof(CanUpdate));
            }
        }
    }

    public string CompanyCountText
    {
        get => _companyCountText;
        set { if (_companyCountText != value) { _companyCountText = value; OnPropertyChanged(); } }
    }

    public string BazeFolderPath
    {
        get => _bazeFolderPath;
        set { if (_bazeFolderPath != value) { _bazeFolderPath = value; OnPropertyChanged(); } }
    }

    public DateTime? LastLaunched
    {
        get => _lastLaunched;
        set { if (_lastLaunched != value) { _lastLaunched = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastLaunchedText)); } }
    }

    public string LastLaunchedText => LastLaunched.HasValue
        ? $"🕒 Pokrenut: {LastLaunched.Value:HH:mm:ss}"
        : "🕒 Nije pokretan u sesiji";

    public string StatusText => Status switch
    {
        ModuleStatus.Running => "🟢 Pokrenut",
        ModuleStatus.UpdateAvailable => $"🟡 Dostupno ažuriranje (v{AvailableVersion})",
        ModuleStatus.Installed => $"🔵 Instaliran (v{InstalledVersion})",
        ModuleStatus.NotInstalled => "⚪ Nije pronađen executable",
        _ => "Nepoznato"
    };

    public bool CanLaunch => Status == ModuleStatus.Installed || Status == ModuleStatus.UpdateAvailable || Status == ModuleStatus.Running;
    public bool CanUpdate => Status == ModuleStatus.UpdateAvailable;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
