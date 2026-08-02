using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ERPiHub.Models;
using Microsoft.Data.Sqlite;

namespace ERPiHub.Services;

public class CompanyService
{
    private readonly string _configFilePath;

    public CompanyService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "ERPiHub");
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
                DbPath = @"C:\ERPi\ERPiFinansije\ERPiFinansijeApp\bin\Debug\net8.0-windows\accounting.db"
            }
        };

        SaveCompanies(defaults);
        return defaults;
    }

    public List<CompanyItem> DiscoverRealCompanies()
    {
        var result = new List<CompanyItem>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Uz nove ERPi foldere traže se i oni pod starim imenima aplikacija: modul svoje
        // baze preuzima u novi folder tek pri prvom pokretanju verzije iz ERPi linije, a do
        // tada podaci stoje na starom mestu. Posle preuzimanja stari folderi ostaju prazni.
        var searchDirectories = new[]
        {
            Path.Combine(localAppData, "ERPiFinansijeApp", "Baze"),
            Path.Combine(localAppData, "ERPiZaradeApp", "Baze"),
            Path.Combine(localAppData, "ERPiSredstvaApp", "Baze"),
            Path.Combine(localAppData, "AccountingApp", "Baze"),
            Path.Combine(localAppData, "PlataApp", "Baze"),
            Path.Combine(localAppData, "SredstvaApp", "Baze"),
            @"C:\KNJIGE\Radni",
            @"C:\ERPi\ERPiFinansije"
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

            // Tabela Firme nema istu šemu u sva tri modula:
            //   Finansije : Sifra, Naziv, Pib, MaticniBroj
            //   Sredstva  : Naziv, PIB, MaticniBroj   (bez Sifra)
            //   Zarade    : Naziv, Pib, Mb            (bez Sifra i MaticniBroj)
            // Zato se prvo čita stvarna šema, pa se upit sastavlja od kolona koje postoje.
            // Ranije je fiksni upit uspevao samo nad Finansijama, a za ostale se naziv
            // firme izvodio iz imena fajla.
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

        // Podrazumevane baze koje moduli sami naprave nisu firme. Uz osnovna imena
        // preskaču se i varijante sa sufiksom `_stara`, koje nastaju kada modul pri
        // preuzimanju podataka zatekne istoimenu bazu u novom folderu.
        var tehnickeBaze = new[] { "accounting", "plata", "sredstva" };
        if (tehnickeBaze.Any(ime =>
                fileName.Equals(ime, StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals($"{ime}_stara", StringComparison.OrdinalIgnoreCase)))
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
