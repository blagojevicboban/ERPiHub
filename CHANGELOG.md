# 📋 CHANGELOG — ErpHub

Sve značajne promene i nova izdanja projekta **ErpHub** biće dokumentovane u ovom fajlu.
Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu.

---

## [1.0.7] - 2026-07-30

### 🏢 Rebrendiranje i Novi Vizuelni Identitet
- **Ažuriran Modul ERPi ZARADE**: Naziv modula preimenovan iz *ERPi Plate* u **`ERPi Zarade`** u `ModuleDiscoveryService` sa prosirenim opisima za ugovore o delu i PP poslove.
- **🎨 Zvanična ERPi HUB Ikonica**: Dodata nova visoko-rezoluciona ikona `app.ico` (motiv poslovne aktovke + ERPi HUB) na plavoj zaobljenoj podlozi (`#2563EB`).

---

## [1.0.6] - 2026-07-29

### 🎨 UI / UX i Usklađivanje Boja
- **Usklađene boje kartica sa aplikacijama**: Gradijenti na karticama modula ažurirani tako da tačno odgovaraju primarnim i akcentnim bojama svake aplikacije pojedinačno:
  - **Finansije (`AccountingApp`)**: `#1E40AF → #2563EB` (plava paleta)
  - **Plata (`PlataApp`)**: `#1A237E → #3949AB` (indigo paleta)
  - **Sredstva (`SredstvaApp`)**: `#1B4332 → #2D6A4F` (zelena paleta)

---

## [1.0.1] - 2026-07-29


### 🚀 Nove Funkcionalnosti & Arhitektura
- **Nezavisni Selektori Baza po Modulima:** Izmešten globalni selektor sa vrha u nezavisne padajuće menije `🏢 Aktivna baza / preduzeće:` na svakoj kartici modula pojedinačno.
- **Autodetekcija Realnih SQLite Baza:** Implementiran SQLite skener u `CompanyService` koji automatski pronalazi i očitava sve realne baze i preduzeća na disku.
- **Prečica za Otvaranje Foldera sa Bazama:** Dodato dugme `📂 Baze` na dnu svake kartice koje jednim klikom otvara folder sa `.db` fajlovima u Windows Explorer-u.

### 🎨 Dizajn i Ikonica
- **Moderna 3D Aplikativna Ikonica (`app.ico`):** Ugrađena brendirana ikonica za `ErpHub.exe` i Velopack `Setup.exe` instalacioni paket.

### ⚡ Performanse i Stabilnost
- **Instant Praćenje Izlaska Procesa:** Poboljšan `Process.Exited` slušalac i `INotifyPropertyChanged` u modelu radi trenutačnog reagovanja pri zatvaranju modula.

---

## [1.0.0] - 2026-07-29

### 🚀 Nove Funkcionalnosti
- **Centralni Control Center UI:** Kreiran je moderan kontrolni panel za brzi pregled i pokretanje svih ERP poslovnih modula (*Finansije & Magacin*, *Obračun Zarada*, *Osnovna Sredstva*).
- **Selektor Preduzeća:** Ugrađen je padajući meni za izbor aktivne firme iz `companies.json` sa prikazom putanje do baze podataka.
- **Kontekstualno Lansiranje Modula:** Dodato automatsko prosleđivanje CLI parametara (`--company-id` i `--db-path`) pri lansiranju modula.
- **Real-Time Praćenje Statusa Modula:** Implementiran `Process.Exited` slušalac i `DispatcherTimer` (3s) za automatsko osvežavanje stanja radnih procesa (Instaliran / Pokrenut / Nije pronađen) bez potrebe za ručnim osvežavanjem.
- **Velopack Auto-Update Integracija:** Ugrađena podrška za automatsko ažuriranje sa GitHub-a uz pop-up dijalog (`UpdateDialog.xaml`) za preuzimanje i restart.
- **Jedinstveni Izvor Istine za Verziju:** Konfigurisano automatsko čitanje verzije iz `version.txt` direktno u MSBuild sklopu.
- **CI/CD & Release Pipeline:** Napravljeni [.github/workflows/release.yml](file:///.github/workflows/release.yml) i [publish.ps1](file:///publish.ps1) skripta za kreiranje `ErpHub-1.0.0-win-x64-Setup.exe` instalacionog paketa.

### 🎨 UI / UX i Odzivnost
- Kartice modula u uniformnoj mreži sa gradijentnim zaglavljima, emotikon ikonama i jasnim statusnim bedževima (`🟢 Pokrenut`, `🔵 Instaliran`, `⚪ Nije pronađen`).
- Dodat `INotifyPropertyChanged` interfejs na `ModuleItem` model radi brze vizuelne reakcije bez treperenja elemenata.
