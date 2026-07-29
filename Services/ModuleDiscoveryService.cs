using System.Diagnostics;
using System.IO;
using ErpHub.Models;

namespace ErpHub.Services;

public class ModuleDiscoveryService
{
    public List<ModuleItem> GetModules()
    {
        var modules = new List<ModuleItem>
        {
            new ModuleItem
            {
                Id = "Accounting",
                Title = "Finansije & Magacin",
                Subtitle = "Glavna knjiga, robno-materijalno, kupci i dobavljači",
                Description = "Kompletan modul za finansijsko knjigovodstvo, nivelacije, popise, robne kartice, izvod banke i bilanse.",
                Icon = "📘",
                HeaderGradientStart = "#1E3C72",
                HeaderGradientEnd = "#2A5298"
            },
            new ModuleItem
            {
                Id = "Plata",
                Title = "Obračun Zarada",
                Subtitle = "Evidencija zaposlenih, bolovanja, obrok/prevoz i PPP-PD",
                Description = "Modul za obračun plata, izradu platnih listića u PDF-u, generisanje XML-a za Poresku upravu i virmana za banku.",
                Icon = "💼",
                HeaderGradientStart = "#11998E",
                HeaderGradientEnd = "#38EF7D"
            },
            new ModuleItem
            {
                Id = "Sredstva",
                Title = "Osnovna Sredstva",
                Subtitle = "Evidencija opreme, MRS 16, Poreski bilans (PB-1 & OA)",
                Description = "Modul za evidenciju osnovnih sredstava, popise, bar-kod nalepnice, revalorizaciju i amortizaciju po MRS 16 i OA pravilu.",
                Icon = "🏢",
                HeaderGradientStart = "#FF8008",
                HeaderGradientEnd = "#FFC837"
            }
        };

        foreach (var mod in modules)
        {
            RefreshModuleStatus(mod);
        }

        return modules;
    }

    public void RefreshModuleStatus(ModuleItem module)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var candidatePaths = module.Id switch
        {
            "Accounting" => new[]
            {
                @"C:\KNJIGE\AccountingSystem\AccountingApp\bin\Debug\net8.0-windows\AccountingApp.exe",
                @"C:\KNJIGE\AccountingSystem\publish_output\AccountingApp.exe",
                Path.Combine(localAppData, "AccountingApp", "AccountingApp.exe")
            },
            "Plata" => new[]
            {
                @"C:\PLATA\PlataSistem\PlataApp\bin\Debug\net8.0-windows\PlataApp.exe",
                @"C:\PLATA\PlataSistem\publish_output\PlataApp.exe",
                Path.Combine(localAppData, "PlataApp", "PlataApp.exe")
            },
            "Sredstva" => new[]
            {
                @"C:\SREDSTVA\SredstvaSystem\SredstvaApp\bin\Debug\net8.0-windows\SredstvaApp.exe",
                @"C:\SREDSTVA\SredstvaSystem\publish_output\SredstvaApp.exe",
                Path.Combine(localAppData, "SredstvaApp", "SredstvaApp.exe")
            },
            _ => Array.Empty<string>()
        };

        string foundPath = string.Empty;
        foreach (var p in candidatePaths)
        {
            if (File.Exists(p))
            {
                foundPath = p;
                break;
            }
        }

        if (string.IsNullOrEmpty(foundPath))
        {
            module.Status = ModuleStatus.NotInstalled;
            module.ExePath = string.Empty;
            module.InstalledVersion = "-";
            return;
        }

        module.ExePath = foundPath;

        try
        {
            var info = FileVersionInfo.GetVersionInfo(foundPath);
            module.InstalledVersion = string.IsNullOrWhiteSpace(info.FileVersion) ? "1.0.0" : info.FileVersion;
        }
        catch
        {
            module.InstalledVersion = "1.0.0";
        }

        var procName = Path.GetFileNameWithoutExtension(foundPath);
        var runningProcs = Process.GetProcessesByName(procName);

        if (runningProcs.Length > 0)
        {
            module.Status = ModuleStatus.Running;
        }
        else
        {
            module.Status = ModuleStatus.Installed;
        }
    }
}
