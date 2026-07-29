using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ErpHub.Models;

namespace ErpHub.Services;

public class UpdateCheckService
{
    private static readonly HttpClient _http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ErpHub-UpdateCheck");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    // Repo za svaki modul mora biti javan (GitHub API za releases/latest ovde ne šalje token).
    private static (string Owner, string Repo)? GetRepo(string moduleId) => moduleId switch
    {
        "Accounting" => ("blagojevicboban", "AccountingSystem"),
        "Plata" => ("blagojevicboban", "PayrollSystem"),
        "Sredstva" => ("blagojevicboban", "AssetManager"),
        _ => null
    };

    public async Task RefreshUpdateStatusAsync(ModuleItem module)
    {
        if (module.Status == ModuleStatus.NotInstalled)
        {
            module.UpdateState = UpdateCheckState.Unknown;
            return;
        }

        var repo = GetRepo(module.Id);
        if (repo == null)
        {
            module.UpdateState = UpdateCheckState.Unknown;
            return;
        }

        module.UpdateState = UpdateCheckState.Checking;

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.github.com/repos/{repo.Value.Owner}/{repo.Value.Repo}/releases/latest");

            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                module.UpdateState = UpdateCheckState.CheckFailed;
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var tagName = doc.RootElement.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var latestVersionText = (tagName ?? string.Empty).TrimStart('v', 'V');

            if (!TryParseVersion(latestVersionText, out var latestVersion) ||
                !TryParseVersion(module.InstalledVersion, out var installedVersion))
            {
                module.UpdateState = UpdateCheckState.CheckFailed;
                return;
            }

            module.AvailableVersion = latestVersionText;
            module.UpdateState = latestVersion > installedVersion
                ? UpdateCheckState.UpdateAvailable
                : UpdateCheckState.UpToDate;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Provera ažurnosti nije uspela za modul {module.Id}: {ex.Message}");
            module.UpdateState = UpdateCheckState.CheckFailed;
        }
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
