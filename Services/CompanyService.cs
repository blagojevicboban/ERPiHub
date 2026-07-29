using System.IO;
using System.Text.Json;
using ErpHub.Models;

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
                // Fallback do zadanih firmi
            }
        }

        // Podrazumevana demo/početna lista firmi
        var defaults = new List<CompanyItem>
        {
            new CompanyItem
            {
                Id = 1,
                Naziv = "PROMET DOO Beograd",
                Pib = "100234567",
                MaticniBroj = "07123456",
                DbPath = @"C:\ERP\AccountingSystem\AccountingApp\bin\Debug\net8.0-windows\accounting.db"
            },
            new CompanyItem
            {
                Id = 2,
                Naziv = "AGROTRADE PR Novi Sad",
                Pib = "108987654",
                MaticniBroj = "20456789",
                DbPath = @"C:\ERP\AccountingSystem\AccountingApp\bin\Debug\net8.0-windows\agrotrade.db"
            }
        };

        SaveCompanies(defaults);
        return defaults;
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
