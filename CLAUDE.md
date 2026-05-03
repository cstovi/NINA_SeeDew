# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

A NINA (Nighttime Imaging 'N' Astronomy) plugin that automatically controls the dew heater on a Seestar telescope. It reads temperature and dew point from NINA's weather mediator and turns the heater on/off via Alpaca REST to prevent dew forming on the optics. Zero Python, zero CLI — installs from NINA's plugin manager.

## Build

```powershell
cd NINA.Plugin.SeeDew
dotnet build                    # Debug
dotnet build -c Release         # Release
```

Output: `bin\Debug\net8.0-windows\NINA.Plugin.SeeDew.dll`

## Install for testing

Copy only `NINA.Plugin.SeeDew.dll` into `%LOCALAPPDATA%\NINA\Plugins\3.0.0\SeeDew\` and restart NINA. NINA loads all plugins from the `3.0.0` subfolder regardless of the running NINA version — do not use the `3.2.0.9001` folder, NINA does not scan it for plugins. No other files from the build output are needed — NINA ships its own dependencies.

## Project Structure

```
NINA.Plugin.SeeDew/
  SeeDewPlugin.cs               ← IPluginManifest + INotifyPropertyChanged; owns DewControlService; options UI bindings
  SeeDewSettings.cs             ← JSON settings persisted to %LOCALAPPDATA%\NINA\Plugins\SeeDew\settings.json
  Services/
    DewControlService.cs        ← polling loop, hysteresis logic, events (CycleCompleted, LogEntryAdded, StatusChanged)
    SeestarAlpacaClient.cs      ← HttpClient wrapper for Alpaca switch endpoints
  ViewModels/
    DewStatusViewModel.cs       ← [Export(typeof(IDockableVM))]; subscribes to service events; Start/Stop commands
  Views/
    DewStatusView.xaml          ← dockable panel: readings grid, heater indicator, log list
  Resources.xaml                ← MEF ResourceDictionary: SeeDew_Options template + dockable data template
  Properties/AssemblyInfo.cs    ← plugin GUID (never change), title, version, tags
```

## Key Architecture Points

**MEF wiring:** NINA uses MEF for DI. `SeeDewPlugin` is exported as both `[Export(typeof(IPluginManifest))]` and `[Export]` so `DewStatusViewModel` can import it by type. `DewStatusViewModel` is exported as `[Export(typeof(IDockableVM))]` and receives `(IProfileService, SeeDewPlugin)` via `[ImportingConstructor]`.

**XAML template keys** must match exactly:
- Options UI: `SeeDew_Options` (matches AssemblyTitle)
- Dockable panel: `NINA.Plugin.SeeDew.ViewModels.DewStatusViewModel_Dockable` (fully qualified VM type + `_Dockable`)

**Control logic** (hysteresis):
```
margin = Temperature - DewPoint
if OFF and margin < OnBelowThreshold  → heater ON
if ON  and margin > OffAboveThreshold → heater OFF
```

**Alpaca endpoints** (base: `http://{host}:{port}/api/v1/switch/0`):
- `GET /getswitch?Id=0` — read heater state
- `PUT /setswitch` — body: `Id=0&State=true/false`
- `PUT /connected` — body: `Connected=true/false`

## NuGet / Target Framework

- Target: `net8.0-windows` (NINA 3.x requires .NET 8)
- Package: `NINA.Plugin 3.2.0.9001` from nuget.org
- NuGet source: `https://api.nuget.org/v3/index.json` — the old MyGet feed (`myget.org/F/isbeorn`) is dead

## Plugin GUID

`1B307E89-2157-4DFB-BFCA-A3ED3A58E272` in `Properties/AssemblyInfo.cs` — never change this after first publish, it's the stable plugin identity.
