using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ErpHub.Models;
using Microsoft.Data.Sqlite;

namespace ErpHub.Services;

public class CompanyService
{
    private readonly string _configFilePath;

    public CompanyService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "ErpHub");
        Directory.CreateDirectory(dir);
        _configFilePath = Path.Combine(dir, "companies.json");
    }

    public List<CompanyItem> GetCompanies()
    {
        // 1. Prvo pokušavamo autodetekciju realnih baza sa diska
        var discovered = DiscoverRealCompanies();
        if (discovered.Count > 0)
        {
            SaveCompanies(discovered);
            return discovered;
        }

        // 2. Ako autodetekcija nije našla baze, proveravamo da li postoji sačuvani companies.json
        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = File.ReadAllText(_configFilePath);
                var list = JsonSerializer.Deserialize<List<CompanyItem>>(json);
                if (list != null && list.Count > 0)
                    return list;
            }
            catch
            {
                // Fallback
            }
        }

        // 3. Podrazumevana demo/početna lista firmi ukoliko nema nikakvih baza na sistemu
        var defaults = new List<CompanyItem>
        {
            new CompanyItem
            {
                Id = 1,
                Naziv = "ARHIBEL d.o.o. Pirot",
                Pib = "100000001",
                MaticniBroj = "07123456",
                DbPath = @"C:\ERP\AccountingSystem\AccountingApp\bin\Debug\net8.0-windows\accounting.db"
            }
        };

        SaveCompanies(defaults);
        return defaults;
    }

    public List<CompanyItem> DiscoverRealCompanies()
    {
        var result = new List<CompanyItem>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var searchDirectories = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AccountingApp", "Baze"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlataApp", "Baze"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SredstvaApp", "Baze"),
            @"C:\KNJIGE\Radni",
            @"C:\ERP\AccountingSystem"
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
            // Ignoriši greške čitanja tabele Firme
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

    public void SaveCompanies(List<CompanyItem> companies)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(companies, options);
            File.WriteAllText(_configFilePath, json);
        }
        catch
        {
            // Ignoriši ili loguj
        }
    }
}
