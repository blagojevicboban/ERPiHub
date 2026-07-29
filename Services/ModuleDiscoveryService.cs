using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ErpHub.Models;
using Microsoft.Data.Sqlite;

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

        // Prava Velopack instalacija (ono što korisnik stvarno pokreće i što se auto-ažurira) uvek
        // ima prioritet nad razvojnim bin/Debug kopijama — živi u %LocalAppData%\<PackId>\current\.
        var candidatePaths = module.Id switch
        {
            "Accounting" => new[]
            {
                Path.Combine(localAppData, "AccountingSystem", "current", "AccountingApp.exe"),
                @"C:\ERP\AccountingSystem\AccountingApp\bin\Debug\net8.0-windows\AccountingApp.exe",
                @"C:\ERP\AccountingSystem\publish_output\AccountingApp.exe",
                @"C:\KNJIGE\AccountingSystem\AccountingApp\bin\Debug\net8.0-windows\AccountingApp.exe",
                Path.Combine(localAppData, "AccountingApp", "AccountingApp.exe")
            },
            "Plata" => new[]
            {
                Path.Combine(localAppData, "PlataSistem", "current", "PlataApp.exe"),
                @"C:\ERP\PlataSistem\PlataSistem\PlataApp\bin\Debug\net8.0-windows\PlataApp.exe",
                @"C:\ERP\PlataSistem\publish_output\PlataApp.exe",
                @"C:\PLATA\PlataSistem\PlataApp\bin\Debug\net8.0-windows\PlataApp.exe",
                Path.Combine(localAppData, "PlataApp", "PlataApp.exe")
            },
            "Sredstva" => new[]
            {
                Path.Combine(localAppData, "SredstvaSystem", "current", "SredstvaApp.exe"),
                @"C:\ERP\SredstvaSystem\SredstvaApp\bin\Debug\net8.0-windows\SredstvaApp.exe",
                @"C:\ERP\SredstvaSystem\publish_output\SredstvaApp.exe",
                @"C:\SREDSTVA\SredstvaSystem\SredstvaApp\bin\Debug\net8.0-windows\SredstvaApp.exe",
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
            module.HasVelopackInstall = false;
            module.UpdateExePath = string.Empty;
            return;
        }

        module.ExePath = foundPath;

        // Prava Velopack instalacija ima oblik <RootAppDir>\current\<App>.exe i <RootAppDir>\Update.exe
        // pored sebe — samo tada ErpHub može da pokrene ažuriranje direktno (vidi UpdateService).
        var currentDir = Path.GetDirectoryName(foundPath);
        var rootAppDir = string.Equals(Path.GetFileName(currentDir), "current", StringComparison.OrdinalIgnoreCase)
            ? Path.GetDirectoryName(currentDir)
            : null;
        var updateExePath = rootAppDir != null ? Path.Combine(rootAppDir, "Update.exe") : string.Empty;

        if (!string.IsNullOrEmpty(updateExePath) && File.Exists(updateExePath))
        {
            module.HasVelopackInstall = true;
            module.UpdateExePath = updateExePath;
        }
        else
        {
            module.HasVelopackInstall = false;
            module.UpdateExePath = string.Empty;
        }

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

        // 1. Očitavanje i populacija specifičnih baza za ovaj modul
        var discoveredCompanies = DiscoverCompaniesForModule(module.Id);
        module.Companies.Clear();
        foreach (var c in discoveredCompanies)
        {
            module.Companies.Add(c);
        }

        if (module.Companies.Count > 0 && module.SelectedCompany == null)
        {
            module.SelectedCompany = module.Companies[0];
        }

        // 2. Izračunavanje statistike baza za modul
        var bazeDir = module.Id switch
        {
            "Accounting" => Path.Combine(localAppData, "AccountingApp", "Baze"),
            "Plata" => Path.Combine(localAppData, "PlataApp", "Baze"),
            "Sredstva" => Path.Combine(localAppData, "SredstvaApp", "Baze"),
            _ => string.Empty
        };

        if (!Directory.Exists(bazeDir) && module.Id == "Plata" && File.Exists(@"C:\ERP\PlataSistem\plata.db"))
        {
            bazeDir = @"C:\ERP\PlataSistem";
        }

        if (Directory.Exists(bazeDir))
        {
            module.BazeFolderPath = bazeDir;
            module.CompanyCountText = module.Companies.Count switch
            {
                0 => "📁 Nema baza",
                1 => "📁 1 baza podataka",
                _ => $"📁 {module.Companies.Count} baza firmi"
            };
        }
        else
        {
            module.BazeFolderPath = string.Empty;
            module.CompanyCountText = "📁 Nema baza";
        }
    }

    public List<CompanyItem> DiscoverCompaniesForModule(string moduleId)
    {
        var result = new List<CompanyItem>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var searchDirectories = moduleId switch
        {
            "Accounting" => new[]
            {
                Path.Combine(localAppData, "AccountingApp", "Baze"),
                @"C:\KNJIGE\Radni"
            },
            "Plata" => new[]
            {
                Path.Combine(localAppData, "PlataApp", "Baze"),
                @"C:\ERP\PlataSistem"
            },
            "Sredstva" => new[]
            {
                Path.Combine(localAppData, "SredstvaApp", "Baze"),
                @"C:\ERP\SredstvaSystem\TestDb"
            },
            _ => Array.Empty<string>()
        };

        int idCounter = 1;

        foreach (var dir in searchDirectories)
        {
            if (!Directory.Exists(dir)) continue;

            var dbFiles = Directory.GetFiles(dir, "*.db", SearchOption.AllDirectories);
            foreach (var dbPath in dbFiles)
            {
                if (dbPath.Contains("_temp", StringComparison.OrdinalIgnoreCase) ||
                    dbPath.Contains("backup", StringComparison.OrdinalIgnoreCase) ||
                    dbPath.Contains("RezervneKopije", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!seenPaths.Add(dbPath)) continue;

                var company = TryReadCompanyFromDb(dbPath, idCounter++);
                if (company != null)
                {
                    result.Add(company);
                }
            }
        }

        return result;
    }

    private CompanyItem? TryReadCompanyFromDb(string dbPath, int id)
    {
        try
        {
            var cs = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ConnectionString;

            using var conn = new SqliteConnection(cs);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Sifra, Naziv, Pib, MaticniBroj FROM Firme LIMIT 1;";

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var sifra = reader.IsDBNull(0) ? "" : reader.GetString(0);
                var naziv = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var pib = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var maticni = reader.IsDBNull(3) ? "" : reader.GetString(3);

                if (!string.IsNullOrWhiteSpace(naziv))
                {
                    return new CompanyItem
                    {
                        Id = id,
                        Sifra = sifra,
                        Naziv = naziv,
                        Pib = string.IsNullOrWhiteSpace(pib) ? "—" : pib,
                        MaticniBroj = maticni,
                        DbPath = dbPath
                    };
                }
            }
        }
        catch
        {
            // Ignoriši ako nema tabele Firme
        }

        var fileName = Path.GetFileNameWithoutExtension(dbPath);
        if (fileName.Equals("accounting", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("plata", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("sredstva", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var cleanName = fileName.Replace("firma_", "").Replace("__", " ").Replace("_", " ").Trim();
        if (string.IsNullOrWhiteSpace(cleanName)) return null;

        return new CompanyItem
        {
            Id = id,
            Sifra = "AUTO",
            Naziv = cleanName,
            Pib = "—",
            MaticniBroj = "—",
            DbPath = dbPath
        };
    }
}
