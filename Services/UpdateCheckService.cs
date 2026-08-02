using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ERPiHub.Models;

namespace ERPiHub.Services;

public class UpdateCheckService
{
    private static readonly HttpClient _http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ERPiHub-UpdateCheck");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    // Repo za svaki modul mora biti javan (GitHub API za releases/latest ovde ne šalje token).
    private static (string Owner, string Repo)? GetRepo(string moduleId) => moduleId switch
    {
        "Accounting" => ("blagojevicboban", "ERPiFinansije"),
        "Plata" => ("blagojevicboban", "ERPiZarade"),
        "Sredstva" => ("blagojevicboban", "ERPiSredstva"),
        _ => null
    };

    public async Task RefreshUpdateStatusAsync(ModuleItem module)
    {
        var repo = GetRepo(module.Id);
        if (repo == null)
        {
            module.UpdateState = UpdateCheckState.Unknown;
            module.InstallCheckState = InstallCheckState.Unknown;
            return;
        }

        // Modul nije instaliran — nema šta da se "ažurira", umesto toga se proverava koja je
        // najnovija verzija dostupna za instalaciju iz huba (Setup.exe asset).
        if (module.Status == ModuleStatus.NotInstalled)
        {
            await RefreshInstallStatusAsync(module, repo.Value);
            return;
        }

        module.UpdateState = UpdateCheckState.Checking;

        try
        {
            var doc = await FetchLatestReleaseAsync(repo.Value);
            if (doc == null)
            {
                module.UpdateState = UpdateCheckState.CheckFailed;
                return;
            }

            using var _ = doc;
            var tagName = doc.RootElement.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var latestVersionText = (tagName ?? string.Empty).TrimStart('v', 'V');

            if (!TryParseVersion(latestVersionText, out var latestVersion) ||
                !TryParseVersion(module.InstalledVersion, out var installedVersion))
            {
                module.UpdateState = UpdateCheckState.CheckFailed;
                return;
            }

            module.AvailableVersion = latestVersionText;
            module.UpdateDownloadUrl = FindAssetUrl(doc.RootElement, "-full.nupkg");
            module.UpdateState = latestVersion > installedVersion
                ? UpdateCheckState.UpdateAvailable
                : UpdateCheckState.UpToDate;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Provera ažurnosti nije uspela za modul {Modul}", module.Id);
            module.UpdateState = UpdateCheckState.CheckFailed;
        }
    }

    private async Task RefreshInstallStatusAsync(ModuleItem module, (string Owner, string Repo) repo)
    {
        module.InstallCheckState = InstallCheckState.Checking;

        try
        {
            var doc = await FetchLatestReleaseAsync(repo);
            if (doc == null)
            {
                module.InstallCheckState = InstallCheckState.CheckFailed;
                return;
            }

            using var _ = doc;
            var tagName = doc.RootElement.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var latestVersionText = (tagName ?? string.Empty).TrimStart('v', 'V');
            var setupUrl = FindAssetUrl(doc.RootElement, "-win-Setup.exe");

            if (string.IsNullOrEmpty(setupUrl))
            {
                module.InstallCheckState = InstallCheckState.CheckFailed;
                return;
            }

            module.AvailableInstallVersion = latestVersionText;
            module.InstallDownloadUrl = setupUrl;
            module.InstallCheckState = InstallCheckState.Available;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Provera dostupne instalacije nije uspela za modul {Modul}", module.Id);
            module.InstallCheckState = InstallCheckState.CheckFailed;
        }
    }

    private async Task<JsonDocument?> FetchLatestReleaseAsync((string Owner, string Repo) repo)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.github.com/repos/{repo.Owner}/{repo.Repo}/releases/latest");

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    // Traži asset čije ime se završava datim sufiksom (npr. "-full.nupkg" za Velopack pun paket
    // koji Update.exe apply može da primeni, ili "-win-Setup.exe" za instalacioni program).
    private static string FindAssetUrl(JsonElement release, string suffix)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return string.Empty;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (name != null && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return asset.TryGetProperty("browser_download_url", out var urlProp) ? urlProp.GetString() ?? string.Empty : string.Empty;
            }
        }

        return string.Empty;
    }

    // System.Version tretira izostavljene delove kao -1 (npr. "1.0" < "1.0.0.0"), pa se
    // svaka verzija normalizuje na tačno 4 dela pre poređenja da bi rezultat bio ispravan.
    private static bool TryParseVersion(string text, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(text)) return false;

        var clean = text.Split('-', '+')[0].Trim();
        var parts = clean.Split('.');
        if (parts.Length == 0) return false;

        var nums = new int[4];
        for (int i = 0; i < 4; i++)
        {
            if (i < parts.Length && int.TryParse(parts[i], out var n))
                nums[i] = n;
        }

        version = new Version(nums[0], nums[1], nums[2], nums[3]);
        return true;
    }
}
