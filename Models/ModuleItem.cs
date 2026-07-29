namespace ErpHub.Models;

public enum ModuleStatus
{
    NotInstalled,
    Installed,
    Running,
    UpdateAvailable
}

public class ModuleItem
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Subtitle { get; set; }
    public required string Description { get; set; }
    public required string Icon { get; set; }
    public required string HeaderGradientStart { get; set; }
    public required string HeaderGradientEnd { get; set; }
    public string ExePath { get; set; } = string.Empty;
    public string InstalledVersion { get; set; } = "1.0.0";
    public string AvailableVersion { get; set; } = string.Empty;
    public ModuleStatus Status { get; set; } = ModuleStatus.NotInstalled;
    public DateTime? LastLaunched { get; set; }
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
}
