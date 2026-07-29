using System.Diagnostics;
using System.IO;
using System.Windows;
using ErpHub.Models;

namespace ErpHub.Services;

public class ModuleLauncherService
{
    public bool LaunchModule(ModuleItem module, CompanyItem? activeCompany)
    {
        if (string.IsNullOrEmpty(module.ExePath) || !File.Exists(module.ExePath))
        {
            MessageBox.Show(
                $"Executable fajl za modul '{module.Title}' nije pronađen na lokaciji:\n{module.ExePath}",
                "Greska pri pokretanju",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = module.ExePath,
                WorkingDirectory = Path.GetDirectoryName(module.ExePath),
                UseShellExecute = true
            };

            if (activeCompany != null)
            {
                psi.Arguments = $"--company-id {activeCompany.Id} --db-path \"{activeCompany.DbPath}\"";
            }

            var proc = Process.Start(psi);
            if (proc != null)
            {
                module.Status = ModuleStatus.Running;
                module.LastLaunched = DateTime.Now;
                return true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Greška pri pokretanju modula '{module.Title}':\n{ex.Message}",
                "Neočekivana greška",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        return false;
    }
}
