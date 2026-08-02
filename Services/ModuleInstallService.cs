using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ERPiHub.Models;

namespace ERPiHub.Services;

public class ModuleInstallResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public readonly record struct ModuleInstallProgress(string Label, int Percent, bool Indeterminate);

// Preuzima Velopack "-win-Setup.exe" instalater i pokreće ga u tihom režimu — isti instalater
// koji korisnik inače preuzima ručno sa GitHub Releases stranice modula.
public class ModuleInstallService
{
    private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

    public async Task<ModuleInstallResult> InstallAsync(ModuleItem module, Action<ModuleInstallProgress>? onProgress = null)
    {
        if (module.Status != ModuleStatus.NotInstalled)
            return new ModuleInstallResult { Success = false, Message = "Modul je već instaliran." };

        if (string.IsNullOrEmpty(module.InstallDownloadUrl))
            return new ModuleInstallResult { Success = false, Message = "Nije pronađen instalacioni paket za preuzimanje." };

        string setupPath;
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ERPiHub-installs");
            Directory.CreateDirectory(tempDir);
            setupPath = Path.Combine(tempDir, $"{module.Id}-{module.AvailableInstallVersion}-Setup.exe");

            onProgress?.Invoke(new ModuleInstallProgress($"Preuzimanje instalacije (v{module.AvailableInstallVersion})... 0%", 0, false));

            using var response = await _http.GetAsync(module.InstallDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using (var httpStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = File.Create(setupPath))
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
                            onProgress?.Invoke(new ModuleInstallProgress($"Preuzimanje instalacije (v{module.AvailableInstallVersion})... {percent}%", percent, false));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return new ModuleInstallResult { Success = false, Message = $"Preuzimanje nije uspelo: {ex.Message}" };
        }

        try
        {
            // Setup.exe ne izveštava progres instalacije — traka prelazi u neodređeni (indeterminate) režim.
            onProgress?.Invoke(new ModuleInstallProgress("Instalacija modula u toku...", 100, true));

            var psi = new ProcessStartInfo
            {
                FileName = setupPath,
                WorkingDirectory = Path.GetDirectoryName(setupPath) ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("--silent");

            using var process = Process.Start(psi);
            if (process == null)
                return new ModuleInstallResult { Success = false, Message = "Instalacioni program nije mogao da se pokrene." };

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return new ModuleInstallResult { Success = false, Message = $"Instalacija je vratila grešku (kod {process.ExitCode})." };

            return new ModuleInstallResult { Success = true, Message = $"Modul '{module.Title}' je uspešno instaliran." };
        }
        catch (Exception ex)
        {
            return new ModuleInstallResult { Success = false, Message = $"Instalacija nije uspela: {ex.Message}" };
        }
        finally
        {
            try { File.Delete(setupPath); } catch { }
        }
    }
}
