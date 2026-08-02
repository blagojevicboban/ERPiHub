# 📋 CHANGELOG — ErpHub

Sve značajne promene i nova izdanja projekta **ErpHub** biće dokumentovane u ovom fajlu.
Format je zasnovan na [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standardu.

## [1.0.15] - 2026-08-02

### 🏢 Prepoznavanje firmi iz svih modula (`ModuleDiscoveryService`)
- **Nazivi firmi se sada čitaju i iz Zarada i iz Sredstava.** Upit nad tabelom `Firme` je bio
  fiksan (`SELECT Sifra, Naziv, Pib, MaticniBroj`) i odgovarao je jedino šemi Finansija —
  Sredstva nemaju kolonu `Sifra`, a Zarade ni `Sifra` ni `MaticniBroj` (koriste `Mb`). Za ta
  dva modula je upit pucao, pa se naziv firme izvodio iz imena fajla; otud su se prikazivali
  kao „AUTO" sa PIB-om „—". Sada se prvo očitava stvarna šema tabele pa se upit sastavlja od
  kolona koje postoje.
- Neuspelo čitanje se više ne guta nego se beleži u log, uz jasnu naznaku da se prelazi na
  izvođenje naziva iz imena fajla.
- **Uklonjene zastarele putanje pretrage**: za Zarade se više ne pretražuje `C:\ERP\PlataSistem`,
  za Sredstva `C:\ERP\SredstvaSystem\TestDb`. Baze modula od sada žive isključivo u
  `%LOCALAPPDATA%\<Modul>App\Baze\`.
- **Arhivirane kopije (`_stara_`) se ne nude za pokretanje** — nastaju pri preseljenju baza u
  ERPi Zarade i imaju isti naziv firme kao aktivna baza, pa bi se lako slučajno radilo u zastareloj.
- Ispravljena putanja do razvojne verzije PlataApp-a nakon spljoštavanja `PlataSistem` repozitorijuma.

---

## [1.0.14] - 2026-08-01

### 🎨 UI / UX
- **Bele ikonice na karticama modula i u naslovnoj traci** — ikonice (📘/💼/🏢/⚡) su se renderovale crno i gubile na tamnim/obojenim pozadinama; sada eksplicitno `Foreground="White"`.
- **ERPi Zarade kartica dodatno zatamnjena/zagasitija** (`#2D1B42` / `#43305F`), usklađeno sa PlataApp v1.1.11.

---

## [1.0.13] - 2026-08-01

### 🎨 UI / UX
- **Zamenjene boje naslovne trake ErpHub-a i kartice ERPi Finansije**: gornja traka ("ERPi Hub — Control Center") sada nosi tamnonavy gradijent (`#0F172A`→`#1E293B`), a kartica ERPi Finansije punu boju (`#1E293B`) — jasnija vizuelna razlika između chrome-a aplikacije i kartice modula.

---

## [1.0.12] - 2026-08-01

### 🎨 UI / UX i Brending
- **Boja kartice ERPi Finansije usklađena sa aplikacijom** (`#0F172A` / `#1E293B`, tamnonavy umesto svetlije plave) — prati stvarnu boju sidebar-a unutar `AccountingApp`.
- **Boja kartice ERPi Zarade dodatno zatamnjena** (`#4C1D95` / `#5B21B6`) — usklađeno sa novom tamnijom ljubičastom paletom u `PlataApp`.
- **Prikazani nazivi instaliranih modula ispravljeni** u `--packTitle`: `ErpHub` → **ERPi Hub**, `AccountingSystem` → **ERPi Finansije**, `SredstvaSystem` → **ERPi Sredstva**, `PlataSistem` → **ERPi Zarade** (samo prikazani naziv u Windows meniju/prečicama; `packId` nepromenjen radi kontinuiteta auto-update-a).

---

## [1.0.11] - 2026-08-01

### 🎨 UI / UX i Sinhronizacija Modula
- **Redizajn Boja i Tematsko Usklađivanje Modula ERPi Zarade (`#5B21B6` / `#7C3AED`)**:
  - Prilagođene gradijentne boje na kartici modula *ERPi Zarade* u `ModuleDiscoveryService` radi savršene vizuelne usklađenosti sa aplikacijom Zarada.
- **Sinhronizacija Lansiranja sa novim verzijama ERP modula**:
  - `AccountingSystem` v1.0.52 (Mesta troška, Blagajna, Putni nalozi, Kompenzacije, Komercijala).
  - `PlataSistem` v1.1.9 (Redizajnirana vizuelna tema Zarada).

---

## [1.0.10] - 2026-08-01

### 🚀 Nove funkcionalnosti i Sinhronizacija
- **Sinhronizacija lansiranja sa AccountingSystem v1.0.44 i SredstvaSystem v1.0.52**:
  - Usklađena detekcija i podrška za nove devizne module, uvozne kalkulacije, DMS arhivu i Poreski Bilans.

---

## [1.0.9] - 2026-07-31

### 🚀 Nove Funkcionalnosti & Arhitektura
- **Integracija preuzimanja i instalacije modula (One-Click Install)**:
  - Ugrađena nova funkcionalnost za direktno preuzimanje i pokretanje instalacionih paketa modula (`-win-Setup.exe`) ukoliko modul nije instaliran na sistemu.
  - Implementiran progres bar za prikaz napretka preuzimanja direktno na karticama modula.
  - Dodat `ModuleInstallService` za preuzimanje paketa sa GitHub-a i automatsko pokretanje instalacije.
- **Deinstalacija Modula (Silent Uninstall)**:
  - Dodata mogućnost tihog uklanjanja (deinstalacije) aplikacija putem ugrađenog Velopack alata (`Update.exe uninstall --silent`), čime se aplikacija potpuno briše sa sistema.

### 🐛 Ispravke
- Usklađeno mapiranje putanja za AccountingApp u `ModuleDiscoveryService`.

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
