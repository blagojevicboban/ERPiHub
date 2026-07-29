using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ErpHub.Models;

public enum ModuleStatus
{
    NotInstalled,
    Installed,
    Running
}

public enum UpdateCheckState
{
    Unknown,
    Checking,
    UpToDate,
    UpdateAvailable,
    CheckFailed
}

public class ModuleItem : INotifyPropertyChanged
{
    private ModuleStatus _status = ModuleStatus.NotInstalled;
    private string _exePath = string.Empty;
    private string _installedVersion = "1.0.0";
    private string _availableVersion = string.Empty;
    private UpdateCheckState _updateState = UpdateCheckState.Unknown;
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
        set { if (_availableVersion != value) { _availableVersion = value; OnPropertyChanged(); OnPropertyChanged(nameof(UpdateStatusText)); } }
    }

    public UpdateCheckState UpdateState
    {
        get => _updateState;
        set { if (_updateState != value) { _updateState = value; OnPropertyChanged(); OnPropertyChanged(nameof(UpdateStatusText)); } }
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
        ModuleStatus.Installed => $"🔵 Instaliran (v{InstalledVersion})",
        ModuleStatus.NotInstalled => "⚪ Nije pronađen executable",
        _ => "Nepoznato"
    };

    public string UpdateStatusText => UpdateState switch
    {
        UpdateCheckState.Checking => "⏳ Provera ažurnosti...",
        UpdateCheckState.UpToDate => "✅ Ažurna verzija",
        UpdateCheckState.UpdateAvailable => $"🟡 Dostupno ažuriranje (v{AvailableVersion})",
        UpdateCheckState.CheckFailed => "❔ Provera nije uspela",
        _ => "—"
    };

    public bool CanLaunch => Status == ModuleStatus.Installed || Status == ModuleStatus.Running;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
