using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ERPiHub.Models;
using Microsoft.Data.Sqlite;

namespace ERPiHub.Services;

public class ModuleDiscoveryService
{
    public List<ModuleItem> GetModules()
    {
        var modules = new List<ModuleItem>
        {
            new ModuleItem
            {
                Id = "Accounting",
                Title = "ERPi Finansije",
                Subtitle = "Glavna knjiga, robno, materijalno i bilansi",
                Description = "Kompletan ERPi modul za finansijsko knjigovodstvo, naloze, robno-materijalno poslovanje, kalkulacije, IOS i APR bilanse.",
                Icon = "📘",
                HeaderGradientStart = "#1E293B",
                HeaderGradientEnd = "#1E293B"
            },
            new ModuleItem
            {
                Id = "Plata",
                Title = "ERPi Zarade",
                Subtitle = "Obračun zarada, ugovori o delu i kadrovska evidencija",
                Description = "Kompletan ERPi modul za obračun zarada, ugovore o delu, PP poslove, kadrovsku evidenciju, platne listiće, XML za Poresku upravu i virmane.",
                Icon = "💼",
                HeaderGradientStart = "#2D1B42",
                HeaderGradientEnd = "#43305F"
            },
            new ModuleItem
            {
                Id = "Sredstva",
                Title = "ERPi Sredstva",
                Subtitle = "Osnovna sredstva i sitan inventar",
                Description = "Kompletan ERPi modul za evidenciju osnovnih sredstava, popise, bar-kod nalepnice, revalorizaciju i amortizaciju po MRS 16 i OA.",
                Icon = "🏢",
                HeaderGradientStart = "#1B4332",
                HeaderGradientEnd = "#2D6A4F"
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
        //
        // Moduli su preimenovani u ERPi* (packId, ime .exe, folderi repozitorijuma). Zbog toga se
        // ovde traže i stara imena: na mašinama gde je modul instaliran pre preimenovanja instalacija
        // i dalje stoji pod starim packId-em (%LocalAppData%\AccountingSystem\current\AccountingApp.exe).
        // Bez ovih fallback putanja hub bi takav modul prikazao kao "nije instaliran".
        var candidatePaths = module.Id switch
        {
            "Accounting" => new[]
            {
                Path.Combine(localAppData, "ERPiFinansije", "current", "ERPiFinansijeApp.exe"),
                Path.Combine(localAppData, "AccountingSystem", "current", "AccountingApp.exe"),
                @"C:\ERPi\ERPiFinansije\ERPiFinansijeApp\bin\Debug\net8.0-windows\ERPiFinansijeApp.exe",
                @"C:\ERPi\ERPiFinansije\publish_output\ERPiFinansijeApp.exe",
                @"C:\KNJIGE\ERPiFinansije\ERPiFinansijeApp\bin\Debug\net8.0-windows\ERPiFinansijeApp.exe",
                Path.Combine(localAppData, "ERPiFinansijeApp", "ERPiFinansijeApp.exe"),
                Path.Combine(localAppData, "AccountingApp", "AccountingApp.exe")
            },
            "Plata" => new[]
            {
                Path.Combine(localAppData, "ERPiZarade", "current", "ERPiZaradeApp.exe"),
                Path.Combine(localAppData, "PlataSistem", "current", "PlataApp.exe"),
                @"C:\ERPi\ERPiZarade\ERPiZaradeApp\bin\Debug\net8.0-windows\ERPiZaradeApp.exe",
                @"C:\ERPi\ERPiZarade\publish_output\ERPiZaradeApp.exe",
                @"C:\PLATA\ERPiZarade\ERPiZaradeApp\bin\Debug\net8.0-windows\ERPiZaradeApp.exe",
                Path.Combine(localAppData, "ERPiZaradeApp", "ERPiZaradeApp.exe"),
                Path.Combine(localAppData, "PlataApp", "PlataApp.exe")
            },
            "Sredstva" => new[]
            {
                Path.Combine(localAppData, "ERPiSredstva", "current", "ERPiSredstvaApp.exe"),
                Path.Combine(localAppData, "SredstvaSystem", "current", "SredstvaApp.exe"),
                @"C:\ERPi\ERPiSredstva\ERPiSredstvaApp\bin\Debug\net8.0-windows\ERPiSredstvaApp.exe",
                @"C:\ERPi\ERPiSredstva\publish_output\ERPiSredstvaApp.exe",
                @"C:\SREDSTVA\ERPiSredstva\ERPiSredstvaApp\bin\Debug\net8.0-windows\ERPiSredstvaApp.exe",
                Path.Combine(localAppData, "ERPiSredstvaApp", "ERPiSredstvaApp.exe"),
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

        // Modul je pronađen na disku — podaci o dostupnoj instalaciji (za NotInstalled karticu)
        // više nisu relevantni dok se eventualno ne deinstalira.
        module.InstallCheckState = InstallCheckState.Unknown;
        module.InstallDownloadUrl = string.Empty;
        module.AvailableInstallVersion = string.Empty;

        // Prava Velopack instalacija ima oblik <RootAppDir>\current\<App>.exe i <RootAppDir>\Update.exe
        // pored sebe — samo tada ERPiHub može da pokrene ažuriranje direktno (vidi UpdateService).
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

        // 2. Izračunavanje statistike baza za modul.
        // Dugme „Baze" vodi u folder u kojem baze stvarno jesu — dok modul ne preuzme
        // podatke iz foldera pod starim imenom, to je još uvek stari folder.
        var bazeDir = module.Id switch
        {
            "Accounting" => PrviFolderSaBazama(
                Path.Combine(localAppData, "ERPiFinansijeApp", "Baze"),
                Path.Combine(localAppData, "AccountingApp", "Baze")),
            "Plata" => PrviFolderSaBazama(
                Path.Combine(localAppData, "ERPiZaradeApp", "Baze"),
                Path.Combine(localAppData, "PlataApp", "Baze")),
            "Sredstva" => PrviFolderSaBazama(
                Path.Combine(localAppData, "ERPiSredstvaApp", "Baze"),
                Path.Combine(localAppData, "SredstvaApp", "Baze")),
            _ => string.Empty
        };

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

    /// <summary>
    /// Vraća prvi od zadatih foldera koji sadrži bar jednu bazu; ako nijedan nema baze,
    /// vraća prvi (novi) folder, jer se tu baze i očekuju.
    /// </summary>
    private static string PrviFolderSaBazama(params string[] kandidati)
    {
        foreach (var dir in kandidati)
        {
            if (Directory.Exists(dir) && Directory.GetFiles(dir, "*.db").Length > 0) return dir;
        }

        return kandidati.Length > 0 ? kandidati[0] : string.Empty;
    }

    public List<CompanyItem> DiscoverCompaniesForModule(string moduleId)
    {
        var result = new List<CompanyItem>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Svaki modul drži baze isključivo u svom %LOCALAPPDATA%\<App>\Baze folderu.
        // Ranije su ovde bile i putanje do izvornog koda (C:\ERPi\...), odakle su se
        // baze i zatekle na pogrešnom mestu; sada su preseljene i te putanje su uklonjene.
        //
        // Uz novi folder traži se i onaj pod starim imenom aplikacije: preimenovanje u ERPi
        // liniju promenilo je ime foldera sa podacima, a modul svoje baze preuzima tek pri
        // prvom pokretanju nove verzije. Do tada bi hub prikazivao „Nema baza" iako baze
        // postoje. Posle preuzimanja stari folder ostaje prazan, pa nema dupliranja.
        var searchDirectories = moduleId switch
        {
            "Accounting" => new[]
            {
                Path.Combine(localAppData, "ERPiFinansijeApp", "Baze"),
                Path.Combine(localAppData, "AccountingApp", "Baze")
            },
            "Plata" => new[]
            {
                Path.Combine(localAppData, "ERPiZaradeApp", "Baze"),
                Path.Combine(localAppData, "PlataApp", "Baze")
            },
            "Sredstva" => new[]
            {
                Path.Combine(localAppData, "ERPiSredstvaApp", "Baze"),
                Path.Combine(localAppData, "SredstvaApp", "Baze")
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
                    dbPath.Contains("RezervneKopije", StringComparison.OrdinalIgnoreCase) ||
                    // Arhivirane kopije zatečene pri preseljenju baza. Ostaju na disku radi
                    // poređenja, ali se ne nude za pokretanje — pod istim su nazivom firme
                    // kao aktivna baza, pa bi se lako slučajno radilo u zastareloj.
                    dbPath.Contains("_stara_", StringComparison.OrdinalIgnoreCase))
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

            // Tabela Firme nema istu šemu u sva tri modula:
            //   Finansije : Sifra, Naziv, Pib, MaticniBroj
            //   Sredstva  : Naziv, PIB, MaticniBroj   (bez Sifra)
            //   Zarade    : Naziv, Pib, Mb            (bez Sifra i MaticniBroj)
            // Zato se prvo čita stvarna šema, pa se upit sastavlja od kolona koje postoje.
            // Ranije je fiksni upit uspevao samo nad Finansijama, a za Sredstva i Zarade
            // je pucao pa se naziv firme izvodio iz imena fajla (otud "AUTO" i PIB "—").
            var kolone = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var pragma = conn.CreateCommand())
            {
                pragma.CommandText = "PRAGMA table_info(Firme);";
                using var pr = pragma.ExecuteReader();
                while (pr.Read()) kolone.Add(pr.GetString(1));
            }

            if (kolone.Count == 0) return IzvediIzNazivaFajla(dbPath, id);

            string? Kolona(params string[] kandidati) =>
                kandidati.FirstOrDefault(k => kolone.Contains(k));

            static string Izraz(string? kolona) => kolona == null ? "''" : $"[{kolona}]";

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"SELECT {Izraz(Kolona("Sifra"))}, {Izraz(Kolona("Naziv"))}, " +
                $"{Izraz(Kolona("Pib", "PIB"))}, {Izraz(Kolona("MaticniBroj", "Mb"))} FROM Firme LIMIT 1;";

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string Citaj(int i) => reader.IsDBNull(i) ? "" : reader.GetValue(i)?.ToString() ?? "";

                var sifra = Citaj(0);
                var naziv = Citaj(1);
                var pib = Citaj(2);
                var maticni = Citaj(3);

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
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Podaci o firmi nisu pročitani iz {Baza} — naziv se izvodi iz imena fajla", dbPath);
        }

        return IzvediIzNazivaFajla(dbPath, id);
    }

    /// <summary>
    /// Rezervni način: naziv firme se izvodi iz imena fajla kada tabela Firme
    /// ne postoji ili je prazna.
    /// </summary>
    private static CompanyItem? IzvediIzNazivaFajla(string dbPath, int id)
    {
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
