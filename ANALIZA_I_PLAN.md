# 🏗️ TEHNIČKA ANALIZA I ARHITEKTONSKI PLAN — ErpHub

Ovaj dokument opisuje tehnički dizajn, arhitekturu, tokove podataka i smernice za razvoj **ErpHub** centralnog poslovnog panela.

---

## 1. Ciljevi i Svrha Sistema

ErpHub ima ulogu centralnog integracionog čvorišta (Launcher / Control Center) čiji su glavni zadaci:
1. **Objedinjeni Pristup:** Omogućava korisniku rad sa svim podsistemima (Finansije, Plate, Osnovna Sredstva) iz jednog mesta.
2. **Konzistentnost Konteksta:** Osigurava da se svi moduli otvaraju nad istom aktivnom firmom/bazom.
3. **Nadzor nad Izvršavanjem:** Omogućava uvid u to koji su moduli trenutno pokrenuti u operativnom sistemu.
4. **Životni Ciklus i Ažuriranja:** Vodi računa o verzijama svih sklopova i obezbeđuje besprekorno ažuriranje (Velopack).

---

## 2. Arhitektonske Komponente

```
                                 ┌─────────────────────────────────┐
                                 │         MainWindow.xaml         │
                                 │     (WPF Navigation & UI)       │
                                 └────────────────┬────────────────┘
                                                  │
                 ┌────────────────────────────────┼────────────────────────────────┐
                 ▼                                ▼                                ▼
┌─────────────────────────────────┐ ┌───────────────────────────┐ ┌─────────────────────────────────┐
│     ModuleDiscoveryService      │ │   ModuleLauncherService   │ │         CompanyService          │
│ - Autodetekcija putanja (.exe)  │ │ - ProcessStartInfo        │ │ - Učitavanje `companies.json`   │
│ - Provera FileVersionInfo       │ │ - Prosleđivanje CLI args  │ │ - Izbor aktivnog preduzeća      │
│ - Provera pokrenutih procesa    │ │ - Process.Exited handler  │ └─────────────────────────────────┘
└─────────────────────────────────┘ └───────────────────────────┘
```

### A. `ModuleDiscoveryService`
Skenira kandidatne lokacije na disku za svaki modul:
- `C:\ERP\<ModuleName>\...` (Razvojni Debug build)
- `publish_output\...` (Lokalni Publish build)
- `%LOCALAPPDATA%\<ModuleName>\...` (Velopack instalirana aplikacija)

### B. `ModuleLauncherService`
Zadužen za pokretanje izabranog `.exe` fajla sa argumentima:
```bash
<ModuleName>.exe --company-id <Id> --db-path "<DbPath>"
```
Sluša događaj `proc.Exited` kako bi odmah vratio status modula na `ModuleStatus.Installed` kada korisnik ugasi prozor.

### C. `CompanyService`
Čita spisak firmi iz fajla `companies.json`. Svako preduzeće sadrži svoj `Id`, `Sifra`, `Naziv`, `Pib` i `DbPath`.

---

## 3. Sistem Ažuriranja (Velopack & GitHub Releases)

- **Inicijalizacija:** U `App.xaml.cs` se na startu poziva `VelopackApp.Build().Run()`.
- **Provera u pozadini:** `MainWindow.xaml.cs` pokreće `CheckForUpdatesAsync()` koji proverava `https://github.com/blagojevicboban/ErpHub`.
- **Instalacija:** Ako postoji noviji release na GitHub-u, prikazuje se `UpdateDialog.xaml` koji vrši preuzimanje paketa i ponovno pokretanje aplikacije.

---

## 4. Razvojne Smernice i Proširenje Modula

Novi moduli se dodaju u `ModuleDiscoveryService.cs` jednostavnim dodavanjem novog `ModuleItem` objekta:

```csharp
new ModuleItem
{
    Id = "NoviModul",
    Title = "Naziv Modula",
    Subtitle = "Kratak opis",
    Description = "Detaljan opis modula...",
    Icon = "📦",
    HeaderGradientStart = "#4A00E0",
    HeaderGradientEnd = "#8E2DE2"
}
```

---

## 5. CI/CD i Verzionisanje

- Verzija se čuva u `version.txt` u korenu projekta.
- Build skripta `publish.ps1` koristi `vpk pack` za generisanje `ErpHub-<Version>-win-x64-Setup.exe`.
- GitHub Action `.github/workflows/release.yml` automatski objavljuje nova izdanja pri push-u na `main` granu.
