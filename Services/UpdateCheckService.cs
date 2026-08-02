using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ERPiHub.Models;

namespace ERPiHub.Services;

public class UpdateCheckService
{
    private static readonly HttpClient _http = CreateClient();

    /// <summary>
    /// Odgovor GitHub-a se pamti nakratko. Bez toga svako „Osveži status" troši tri poziva
    /// od 60 koliko GitHub dozvoljava neautorizovano po IP adresi na sat, pa nekoliko
    /// osvežavanja zaredom obori proveru za ceo sat.
    /// </summary>
    private static readonly Dictionary<string, (DateTime Vreme, string Json)> _kes = new();

    private static readonly TimeSpan _trajanjeKesa = TimeSpan.FromMinutes(10);

    /// <summary>Kada se GitHub kvota obnavlja; postavlja se tek kada se na nju naiđe.</summary>
    private static DateTime? _kvotaDo;

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
            var (doc, ograniceno) = await FetchLatestReleaseAsync(repo.Value);
            if (doc == null)
            {
                if (ograniceno) ZabeleziOgranicenje(module);
                module.UpdateState = ograniceno ? UpdateCheckState.RateLimited : UpdateCheckState.CheckFailed;
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
            var (doc, ograniceno) = await FetchLatestReleaseAsync(repo);
            if (doc == null)
            {
                if (ograniceno) ZabeleziOgranicenje(module);
                module.InstallCheckState = ograniceno ? InstallCheckState.RateLimited : InstallCheckState.CheckFailed;
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

    private async Task<(JsonDocument? Doc, bool Ograniceno)> FetchLatestReleaseAsync((string Owner, string Repo) repo)
    {
        var kljuc = $"{repo.Owner}/{repo.Repo}";

        lock (_kes)
        {
            if (_kes.TryGetValue(kljuc, out var zapamceno) &&
                DateTime.UtcNow - zapamceno.Vreme < _trajanjeKesa)
            {
                return (JsonDocument.Parse(zapamceno.Json), false);
            }
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.github.com/repos/{repo.Owner}/{repo.Repo}/releases/latest");

        using var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            if (JePotrosenaKvota(response))
            {
                // Kad je kvota potrošena, zapamćeni odgovor je bolji od nikakvog —
                // makar i stariji od deset minuta.
                lock (_kes)
                {
                    if (_kes.TryGetValue(kljuc, out var zapamceno))
                    {
                        return (JsonDocument.Parse(zapamceno.Json), false);
                    }
                }

                return (null, true);
            }

            return (null, false);
        }

        var json = await response.Content.ReadAsStringAsync();

        lock (_kes)
        {
            _kes[kljuc] = (DateTime.UtcNow, json);
        }

        return (JsonDocument.Parse(json), false);
    }

    /// <summary>
    /// GitHub na potrošenu kvotu vraća 403/429 sa zaglavljem `x-ratelimit-remaining: 0`
    /// i vremenom obnavljanja u `x-ratelimit-reset` (Unix sekunde).
    /// </summary>
    private static bool JePotrosenaKvota(HttpResponseMessage response)
    {
        if (response.StatusCode != System.Net.HttpStatusCode.Forbidden &&
            response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
        {
            return false;
        }

        if (!response.Headers.TryGetValues("x-ratelimit-remaining", out var preostalo) ||
            preostalo.FirstOrDefault() != "0")
        {
            return false;
        }

        if (response.Headers.TryGetValues("x-ratelimit-reset", out var reset) &&
            long.TryParse(reset.FirstOrDefault(), out var sekunde))
        {
            _kvotaDo = DateTimeOffset.FromUnixTimeSeconds(sekunde).LocalDateTime;
        }

        return true;
    }

    private static void ZabeleziOgranicenje(ModuleItem module)
    {
        module.RateLimitResetText = _kvotaDo.HasValue ? _kvotaDo.Value.ToString("HH:mm") : string.Empty;
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
