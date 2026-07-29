# 🚀 ErpHub — Poslovni Sistem Control Center

![Version](https://img.shields.io/badge/version-1.0.1-blue.svg)
![NET](https://img.shields.io/badge/.NET-8.0--windows-purple.svg)
![UI](https://img.shields.io/badge/UI-WPF%20%7C%20Modern%20Design-success.svg)
![Updater](https://img.shields.io/badge/Auto--Update-Velopack-orange.svg)

**ErpHub** je centralna upravljačka aplikacija (Control Center / Launcher) celokupnog ERP poslovnog sistema. Omogućava brzu navigaciju, nadzor radnog statusa modula u realnom vremenu, upravljanje preduzećima i sinhronizovano lansiranje poslovnih aplikacija sa prosleđivanjem konteksta aktivne baze podataka.

---

## 🏛️ Pregled Arhitekture Sistema

ERP sistem je koncipiran kao modularni poslovni paket sastavljen od centralnog hub-a i tri specijalizovane aplikacije:

```
                          ┌────────────────────────┐
                          │   🚀 ErpHub (v1.0.0)   │
                          │     Control Center     │
                          └───────────┬────────────┘
                                      │
           ┌──────────────────────────┼──────────────────────────┐
           ▼                          ▼                          ▼
┌──────────────────────┐   ┌──────────────────────┐   ┌──────────────────────┐
│ 📘 AccountingSystem  │   │   💼 PlataSistem     │   │  🏢 SredstvaSystem   │
│ Finansije & Magacin  │   │   Obračun Zarada     │   │   Osnovna Sredstva   │
└──────────────────────┘   └──────────────────────┘   └──────────────────────┘
```

---

## 🌟 Ključne Funkcionalnosti ErpHub-a

1. **Centralni Launcher & Autodetekcija Modula:**
   - Automatski skenira lokacije modula u `Debug`, `publish_output` i `%LOCALAPPDATA%` direktorijumima.
   - Čita verzionisanje instaliranih sklopova u realnom vremenu.

2. **Upravljanje Aktivnim Preduzećem (Company Context):**
   - Centralizovani izbor aktivne firme/preduzeća iz `companies.json`.
   - Automatsko prosleđivanje CLI parametara (`--company-id` i `--db-path`) prilikom lansiranja modula.

3. **Praćenje Statusa u Realnom Vremenu (Real-Time Process Monitoring):**
   - Detekcija stanja modula: `⚪ Nije pronađen`, `🔵 Instaliran`, `🟢 Pokrenut`, `🟡 Dostupno ažuriranje`.
   - Reagovanje na `Process.Exited` događaj i periodični pozadinski tajmer za osvežavanje statusa odmah po zatvaranju modula.

4. **Automatsko Ažuriranje (Velopack & GitHub Releases):**
   - Ugrađen Velopack mehanizam za automatsku proveru i instalaciju novih verzija ERP Hub-a sa GitHub-a.
   - Integrisani napredni dijalog za preuzimanje i primenu ažuriranja bez prekida rada.

---

## 🛠️ Tehnološki Stog

- **Framework:** .NET 8.0 WPF (Windows Desktop)
- **Pattern:** MVVM / Service-Driven Architecture
- **Packaging & Updates:** Velopack (`vpk`)
- **CI/CD:** GitHub Actions (`.github/workflows/release.yml`)
- **IDE Support:** VS Code (`launch.json` sa compound lansiranjem svih 4 aplikacija) & Visual Studio 2022

---

## 🚀 Pokretanje i Razvoj

### Pokretanje iz VS Code-a
Pritisnite **`F5`** ili otvorite **Run and Debug** panel i izaberite:
- **`ErpHub (Debug)`** — pokreće samo ERP Hub.
- **`Pokreni sve 4 aplikacije`** — lansira ERP Hub i sva 3 modula u jednoj operaciji.

### Kompajliranje putem CLI-ja
```bash
dotnet build ErpHub.csproj
```

### Pravljenje Instalacionog Paketa (Velopack Setup)
```powershell
.\publish.ps1
```
Skripta kreira instalacioni paket u direktorijumu `ReleasePackage\ErpHub-1.0.0-win-x64-Setup.exe`.

---

## 📝 Konfiguracija Preduzeća (`companies.json`)

Konfiguracioni fajl se nalazi u direktorijumu aplikacije i definiše spisak preduzeća i putanje do SQLite baza podataka:

```json
[
  {
    "Id": "1",
    "Sifra": "F001",
    "Naziv": "ARHSTO d.o.o.",
    "Pib": "100000001",
    "DbPath": "C:\\KNJIGE\\AccountingSystem\\baza.db"
  }
]
```

---

## 📄 Licenca

Copyright © Blagojević Boban 2026. Sva prava zadržana.
