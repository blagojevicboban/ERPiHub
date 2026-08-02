using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ERPiHub.Models;

namespace ERPiHub.Services;

public class ModuleUpdateResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public readonly record struct ModuleUpdateProgress(string Label, int Percent, bool Indeterminate);

// Preuzima Velopack "-full.nupkg" paket i primenjuje ga pozivom Update.exe apply — istog
// samostalnog alata koji svaki modul interno koristi za sopstveno ažuriranje (Update.exe apply
// --package <FILE> je dokumentovan CLI, isporučen uz svaku Velopack instalaciju).
public class ModuleUpdateService
{
    private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

    public async Task<ModuleUpdateResult> ApplyUpdateAsync(ModuleItem module, Action<ModuleUpdateProgress>? onProgress = null)
    {
        if (!module.HasVelopackInstall || string.IsNullOrEmpty(module.UpdateExePath))
            return new ModuleUpdateResult { Success = false, Message = "Modul nije prava Velopack instalacija — ažuriranje iz huba nije moguće." };

        if (string.IsNullOrEmpty(module.UpdateDownloadUrl))
            return new ModuleUpdateResult { Success = false, Message = "Nije pronađen paket za preuzimanje najnovije verzije." };

        if (module.Status == ModuleStatus.Running)
            return new ModuleUpdateResult { Success = false, Message = "Zatvorite modul pre ažuriranja iz huba." };

        string packagePath;
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ERPiHub-updates");
            Directory.CreateDirectory(tempDir);
            packagePath = Path.Combine(tempDir, $"{module.Id}-{module.AvailableVersion}.nupkg");

            onProgress?.Invoke(new ModuleUpdateProgress($"Preuzimanje ažuriranja (v{module.AvailableVersion})... 0%", 0, false));

            using var response = await _http.GetAsync(module.UpdateDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using (var httpStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = File.Create(packagePath))
            {
                var buffer = new byte[81920];
                long totalRead = 0;
                int read;
                var lastReportedPercent = -1;

                while ((read = await httpStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read));
                    totalRead += read;

                    if (totalBytes is > 0)
                    {
                        var percent = (int)(totalRead * 100 / totalBytes.Value);
                        if (percent != lastReportedPercent)
                        {
                            lastReportedPercent = percent;
                            onProgress?.Invoke(new ModuleUpdateProgress($"Preuzimanje ažuriranja (v{module.AvailableVersion})... {percent}%", percent, false));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return new ModuleUpdateResult { Success = false, Message = $"Preuzimanje nije uspelo: {ex.Message}" };
        }

        try
        {
            // Update.exe ne izveštava progres primene — traka prelazi u neodređeni (indeterminate) režim.
            onProgress?.Invoke(new ModuleUpdateProgress("Primena ažuriranja i restart modula...", 100, true));

            var psi = new ProcessStartInfo
            {
                FileName = module.UpdateExePath,
                WorkingDirectory = Path.GetDirectoryName(module.UpdateExePath) ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("apply");
            psi.ArgumentList.Add("--package");
            psi.ArgumentList.Add(packagePath);
            psi.ArgumentList.Add("--silent");

            using var process = Process.Start(psi);
            if (process == null)
                return new ModuleUpdateResult { Success = false, Message = "Update.exe nije mogao da se pokrene." };

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return new ModuleUpdateResult { Success = false, Message = $"Update.exe je vratio grešku (kod {process.ExitCode})." };

            return new ModuleUpdateResult { Success = true, Message = $"Modul '{module.Title}' je ažuriran na v{module.AvailableVersion}." };
        }
        catch (Exception ex)
        {
            return new ModuleUpdateResult { Success = false, Message = $"Primena ažuriranja nije uspela: {ex.Message}" };
        }
        finally
        {
            try { File.Delete(packagePath); } catch { }
        }
    }

    // Poziva Update.exe uninstall --silent — isti alat koji je isporučen uz svaku Velopack
    // instalaciju uklanja prečice, fajlove i registarske unose bez dijaloga.
    public async Task<ModuleUpdateResult> UninstallAsync(ModuleItem module)
    {
        if (!module.HasVelopackInstall || string.IsNullOrEmpty(module.UpdateExePath))
            return new ModuleUpdateResult { Success = false, Message = "Modul nije prava Velopack instalacija — deinstalacija iz huba nije moguća." };

        if (module.Status == ModuleStatus.Running)
            return new ModuleUpdateResult { Success = false, Message = "Zatvorite modul pre deinstalacije." };

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = module.UpdateExePath,
                WorkingDirectory = Path.GetDirectoryName(module.UpdateExePath) ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("uninstall");
            psi.ArgumentList.Add("--silent");

            using var process = Process.Start(psi);
            if (process == null)
                return new ModuleUpdateResult { Success = false, Message = "Update.exe nije mogao da se pokrene." };

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return new ModuleUpdateResult { Success = false, Message = $"Update.exe je vratio grešku (kod {process.ExitCode})." };

            return new ModuleUpdateResult { Success = true, Message = $"Modul '{module.Title}' je deinstaliran." };
        }
        catch (Exception ex)
        {
            return new ModuleUpdateResult { Success = false, Message = $"Deinstalacija nije uspela: {ex.Message}" };
        }
    }
}
