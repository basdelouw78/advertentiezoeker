# Advertentiezoeker

Een .NET MAUI-app (Android + Windows, met Core-logica die overal werkt) die
periodiek Marktplaats.nl controleert op nieuwe advertenties die aan jouw
zoekopdrachten voldoen, en je waarschuwt via een pop-upmelding op je telefoon
en/of een e-mail.

## Wat kun je instellen per zoekopdracht?

- **Steekwoorden** — zoals je ze ook in de Marktplaats-zoekbalk zou typen.
- **Staat** — Nieuw, Zo goed als nieuw, Gebruikt, Refurbished, Niet werkend
  (meerdere tegelijk mogelijk; niets aanvinken = alle staten).
- **Postcode + maximale afstand (km)** — Marktplaats filtert dan zelf op
  afstand tot dat postcodegebied.

Je kunt meerdere zoekopdrachten tegelijk laten draaien. Standaard wordt elke
**30 minuten** gecontroleerd (instelbaar in het instellingenscherm, minimaal
5 minuten).

Bij het toevoegen van een nieuwe zoekopdracht wordt de eerste controle gebruikt
om een startpunt vast te leggen: je krijgt daarvoor geen pop-up of e-mail (dat
zou een lawine aan meldingen geven voor advertenties die er al lang stonden),
maar de op dat moment gevonden advertenties verschijnen wél meteen op het
"Gevonden"-scherm, als directe bevestiging dat de zoekopdracht werkt. Vanaf de
tweede controle krijg je ook pop-up/e-mail voor advertenties die sindsdien
nieuw zijn verschenen.

## Projectstructuur

```
AdvertentieZoeker.sln
src/
  AdvertentieZoeker.Core/     Platformonafhankelijke logica (geen MAUI-afhankelijkheid)
    Models/                   Listing, SavedSearch, Condition, AppSettings, ...
    Services/
      MarktplaatsClient.cs    Praat met de (onofficiële) zoek-API van marktplaats.nl
      MonitorService.cs       Bepaalt welke advertenties nieuw zijn en meldt ze
      EmailNotifier.cs        Verstuurt e-mail via SMTP (MailKit)
      Json*Repository.cs      Opslag van zoekopdrachten/instellingen/gezien-lijst
  AdvertentieZoeker.App/      De .NET MAUI-app zelf (UI + platformcode)
    Views/                    Zoekopdrachten, Zoekopdracht bewerken, Instellingen, Gevonden
    Services/
      PollingService.cs       De controle-lus (elke N minuten)
      PopupNotifier.cs        Lokale pop-upmelding (rechtstreeks native Android/Windows-API, geen plugin)
      FoundListingsLog.cs     Geschiedenis voor het "Gevonden"-scherm
      SecureAppSettingsRepository.cs   SMTP-wachtwoord via beveiligde opslag
    Platforms/Android/
      PollingForegroundService.cs      Houdt de controle actief op de achtergrond
tests/
  AdvertentieZoeker.Core.Tests/        Unit tests voor de Core-laag (xUnit)
```

De Core-laag is bewust MAUI-vrij gehouden en heeft **18 unit tests** die de
querybouw, JSON-parsing en "wat is nieuw"-logica dekken.

## Bouwen

### Alleen de Core-logica + tests (geen mobiele SDK's nodig)

```bash
dotnet test
```

Dit is ook precies wat in deze omgeving getest is: de Marktplaats-client,
datumparser en meldingslogica bouwen en slagen (18/18 tests groen).

### De hele mobiele app bouwen/draaien

Daarvoor heb je de **.NET MAUI workload** nodig, die in deze sandbox-omgeving
niet geïnstalleerd kon worden (geen Android SDK/emulator hier beschikbaar).
Op je eigen machine:

```bash
dotnet workload install maui
dotnet build src/AdvertentieZoeker.App -f net8.0-android      # Android
dotnet build src/AdvertentieZoeker.App -f net8.0-windows10.0.19041.0   # Windows
```

Of open `AdvertentieZoeker.sln` in **Visual Studio 2022** (met de ".NET MAUI"
workload aangevinkt in de installer) en druk op F5 met een Android-emulator
of je eigen telefoon (via USB-debugging) als doel.

> **Let op:** de App-laag (UI, Android-achtergrondservice, pop-upmeldingen)
> is met zorg geschreven volgens de standaard .NET MAUI-conventies, maar kon
> in deze omgeving niet gecompileerd worden omdat de mobiele workloads hier
> niet te installeren waren. Reken er bij de eerste build op je eigen machine
> op dat je nog een enkel typefoutje kunt tegenkomen — de Core-laag met alle
> business-logica is wel volledig gebouwd en getest.

## E-mail instellen

In het scherm **Instellingen** vul je je SMTP-gegevens in (bijvoorbeeld
Gmail: `smtp.gmail.com`, poort 587, STARTTLS aan). Gebruik een
**app-wachtwoord** in plaats van je gewone wachtwoord — bij Gmail maak je die
aan via je Google-accountbeveiliging (2FA moet aanstaan). Het wachtwoord
wordt bewaard via de beveiligde opslag van je besturingssysteem (Android
Keystore / Windows), niet in platte tekst.

## Belangrijke kanttekeningen

- **Onofficiële API.** De app gebruikt dezelfde (niet-publieke, niet-
  gedocumenteerde) zoek-API die marktplaats.nl zelf intern gebruikt. Die kan
  zonder aankondiging veranderen, wat de app op enig moment kan breken.
  Gebruik dit voor persoonlijk gebruik; stuur niet massaal/agressief
  verzoeken (de standaard interval van 30 minuten is daar bewust gekozen).
- **iOS niet volwaardig ondersteund.** Achtergrondverwerking is op iOS zwaar
  beperkt door Apple; een betrouwbare controle elke 30 minuten terwijl de app
  dicht is, is daar niet realistisch te garanderen. Android en Windows zijn
  de volwaardig uitgewerkte platformen.
- **Android-achtergrondservice.** Op Android start de app een
  "foreground service" met een permanente, onopvallende melding
  ("Advertentiezoeker actief") — dat is verplicht op Android om
  achtergrondwerk betrouwbaar te laten doorlopen.
