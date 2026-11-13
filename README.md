# DICOM Prehliadač - Zadanie 1

**Autor:** [Tvoje Meno a Priezvisko]

Táto aplikácia slúži na jednoduché prezeranie DICOM súborov. Zobrazuje hlavičku súboru podľa hierarchie (Pacient, Štúdia, Séria, Snímka), umožňuje zobrazenie obrazu s nastaviteľným oknom (Windowing) a ponúka možnosť uložiť anonymizovanú kópiu súboru.

## Prostredie
* **Operačný systém:** Windows 11
* **Jazyk:** C# (.NET 8.0)
* **Framework:** WPF (Windows Presentation Foundation)

## Závislosti
Projekt využíva nasledujúce NuGet balíčky:
* `fo-dicom` (Fellow Oak DICOM)
* `fo-dicom.Imaging.Desktop`

## Inštalácia a spustenie

**Krok 1: Získanie zdrojového kódu**
Máte dve možnosti, ako získať projekt do svojho počítača:
* **Možnosť A (Najjednoduchšia):** Kliknite na tlačidlo **Code** (modré tlačidlo vpravo hore na tejto stránke) a zvoľte **Download source code -> ZIP**. Stiahnutý ZIP súbor rozbaľte do ľubovoľného priečinka.
* **Možnosť B (Cez Git):** Ak máte nainštalovaný Git, otvorte terminál (Príkazový riadok) a spustite príkaz:
  `git clone https://gitlab.kicon.fri.uniza.sk/cedzo4/medicinska_informatika_zadanie.git`

**Krok 2: Spustenie aplikácie**
1. Uistite sa, že máte nainštalované **Visual Studio 2022** s pracovným prostredím *.NET Desktop Development*.
2. Vo vašom počítači prejdite do priečinka s projektom a otvorte súbor riešenia `DICOMviewer.slnx`.
3. **Dôležité:** Pri prvom otvorení sa môže zobraziť bezpečnostné okno s otázkou, či chcete dôverovať autorom. Kliknite na **"Trust the authors"** (Dôverovať autorom), aby Visual Studio mohlo bezpečne načítať projekt.
4. Visual Studio automaticky stiahne NuGet balíčky na pozadí (vpravo dole môžete vidieť malý indikátor). Počkajte, kým sa balíčky úplne stiahnu. (Ak nie, kliknite pravým tlačidlom myši na názov projektu a zvoľte *Restore NuGet Packages*).
5. Spustite aplikáciu stlačením klávesy **F5** na klávesnici (alebo zeleným tlačidlom *Start* hore).
6. V aplikácii kliknite na tlačidlo **"Otvorit DICOM subor"** a vyberte ľubovoľný `.dcm` súbor (napr. testovacie súbory z knižnice pydicom alebo snímky zo štúdie PHENIX).

## Snímky obrazovky
Snímky obrazovky demonštrujúce funkčnosť aplikácie sa nachádzajú v priečinku `Screenshots/`:
1. **Hlavička súboru** (napr. `CT_small.dcm`)
2. **Snímka štúdie PHENIX** zobrazená v kosťnom okne
3. **Anonymizovaná kópia** otvorená v aplikácii

---
*Vytvorené ako semestrálne zadanie z predmetu Medicínska informatika (Zadanie 1).*
