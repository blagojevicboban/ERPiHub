using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ERPiHub.Models;

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

public enum InstallCheckState
{
    Unknown,
    Checking,
    Available,
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
    private bool _hasVelopackInstall;
    private string _updateExePath = string.Empty;
    private string _updateDownloadUrl = string.Empty;
    private bool _isUpdating;
    private int _updateProgressPercent;
    private bool _updateIsIndeterminate;
    private string _updateProgressLabel = string.Empty;
    private string _availableInstallVersion = string.Empty;
    private string _installDownloadUrl = string.Empty;
    private InstallCheckState _installCheckState = InstallCheckState.Unknown;
    private bool _isInstalling;
    private int _installProgressPercent;
    private bool _installIsIndeterminate;
    private string _installProgressLabel = string.Empty;
    private bool _isUninstalling;

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
        set { if (_availableVersion != value) { _availableVersion = value; OnPropertyChanged(); OnPropertyChanged(nameof(UpdateStatusText)); OnPropertyChanged(nameof(UpdateButtonText)); } }
    }

    public UpdateCheckState UpdateState
    {
        get => _updateState;
        set { if (_updateState != value) { _updateState = value; OnPropertyChanged(); OnPropertyChanged(nameof(UpdateStatusText)); OnPropertyChanged(nameof(CanUpdateFromHub)); OnPropertyChanged(nameof(ShowUpdateArea)); } }
    }

    // Postavlja ModuleDiscoveryService kada pronađeni exe leži u pravoj Velopack "current\" instalaciji
    // (ima Update.exe u root folderu instalacije) — samo tada je moguće ažuriranje direktno iz huba.
    public bool HasVelopackInstall
    {
        get => _hasVelopackInstall;
        set { if (_hasVelopackInstall != value) { _hasVelopackInstall = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanUpdateFromHub)); OnPropertyChanged(nameof(ShowUpdateArea)); OnPropertyChanged(nameof(CanUninstall)); } }
    }

    public string UpdateExePath
    {
        get => _updateExePath;
        set { if (_updateExePath != value) { _updateExePath = value; OnPropertyChanged(); } }
    }

    public string UpdateDownloadUrl
    {
        get => _updateDownloadUrl;
        set { if (_updateDownloadUrl != value) { _updateDownloadUrl = value; OnPropertyChanged(); } }
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        set { if (_isUpdating != value) { _isUpdating = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanUpdateFromHub)); OnPropertyChanged(nameof(ShowUpdateArea)); OnPropertyChanged(nameof(UpdateButtonText)); OnPropertyChanged(nameof(CanUninstall)); } }
    }

    public int UpdateProgressPercent
    {
        get => _updateProgressPercent;
        set { if (_updateProgressPercent != value) { _updateProgressPercent = value; OnPropertyChanged(); } }
    }

    public bool UpdateIsIndeterminate
    {
        get => _updateIsIndeterminate;
        set { if (_updateIsIndeterminate != value) { _updateIsIndeterminate = value; OnPropertyChanged(); } }
    }

    public string UpdateProgressLabel
    {
        get => _updateProgressLabel;
        set { if (_updateProgressLabel != value) { _updateProgressLabel = value; OnPropertyChanged(); } }
    }

    public string UpdateButtonText => IsUpdating
        ? "⏳ Ažuriranje u toku..."
        : $"⬆ Ažuriraj na v{AvailableVersion}";

    // Dugme/traka ostaju vidljivi (samo onemogućeni) tokom ažuriranja umesto da nestanu čim
    // CanUpdateFromHub postane false — inače bi progress bar nestao usred ažuriranja.
    public bool ShowUpdateArea => HasVelopackInstall
        && (UpdateState == UpdateCheckState.UpdateAvailable || IsUpdating);

    public string AvailableInstallVersion
    {
        get => _availableInstallVersion;
        set { if (_availableInstallVersion != value) { _availableInstallVersion = value; OnPropertyChanged(); OnPropertyChanged(nameof(InstallStatusText)); OnPropertyChanged(nameof(InstallButtonText)); } }
    }

    public string InstallDownloadUrl
    {
        get => _installDownloadUrl;
        set { if (_installDownloadUrl != value) { _installDownloadUrl = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanInstallFromHub)); } }
    }

    public InstallCheckState InstallCheckState
    {
        get => _installCheckState;
        set { if (_installCheckState != value) { _installCheckState = value; OnPropertyChanged(); OnPropertyChanged(nameof(InstallStatusText)); OnPropertyChanged(nameof(CanInstallFromHub)); } }
    }

    public bool IsInstalling
    {
        get => _isInstalling;
        set { if (_isInstalling != value) { _isInstalling = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanInstallFromHub)); OnPropertyChanged(nameof(InstallButtonText)); OnPropertyChanged(nameof(CanUninstall)); } }
    }

    public int InstallProgressPercent
    {
        get => _installProgressPercent;
        set { if (_installProgressPercent != value) { _installProgressPercent = value; OnPropertyChanged(); } }
    }

    public bool InstallIsIndeterminate
    {
        get => _installIsIndeterminate;
        set { if (_installIsIndeterminate != value) { _installIsIndeterminate = value; OnPropertyChanged(); } }
    }

    public string InstallProgressLabel
    {
        get => _installProgressLabel;
        set { if (_installProgressLabel != value) { _installProgressLabel = value; OnPropertyChanged(); } }
    }

    public bool IsUninstalling
    {
        get => _isUninstalling;
        set { if (_isUninstalling != value) { _isUninstalling = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanUninstall)); OnPropertyChanged(nameof(UninstallButtonText)); } }
    }

    public string InstallButtonText => IsInstalling
        ? "⏳ Instalacija u toku..."
        : $"⬇ Instaliraj Modul (v{AvailableInstallVersion})";

    public string UninstallButtonText => IsUninstalling
        ? "⏳ Deinstalacija u toku..."
        : "🗑 Deinstaliraj modul";

    public string InstallStatusText => InstallCheckState switch
    {
        InstallCheckState.Checking => "⏳ Provera dostupne verzije...",
        InstallCheckState.Available => $"⬇ Dostupna instalacija (v{AvailableInstallVersion})",
        InstallCheckState.CheckFailed => "❔ Provera nije uspela",
        _ => "Modul nije instaliran na ovom računaru."
    };

    public bool IsNotInstalled => Status == ModuleStatus.NotInstalled;

    public bool ShowLaunchRow => Status != ModuleStatus.NotInstalled;

    public bool CanInstallFromHub => Status == ModuleStatus.NotInstalled
        && !string.IsNullOrEmpty(InstallDownloadUrl)
        && !IsInstalling;

    // Deinstalacija ide preko Update.exe iz Velopack instalacije, pa je moguća samo ako je
    // modul otkriven kao prava Velopack instalacija i trenutno nije pokrenut.
    public bool CanUninstall => HasVelopackInstall
        && Status != ModuleStatus.Running
        && !IsUninstalling
        && !IsUpdating
        && !IsInstalling;

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
                OnPropertyChanged(nameof(CanUpdateFromHub));
                OnPropertyChanged(nameof(IsNotInstalled));
                OnPropertyChanged(nameof(ShowLaunchRow));
                OnPropertyChanged(nameof(CanInstallFromHub));
                OnPropertyChanged(nameof(CanUninstall));
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

    public bool CanUpdateFromHub => HasVelopackInstall
        && UpdateState == UpdateCheckState.UpdateAvailable
        && Status != ModuleStatus.Running
        && !IsUpdating;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
