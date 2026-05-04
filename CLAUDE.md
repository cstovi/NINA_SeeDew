# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

@../NINA_shared/NINA_plugin_guide.md

## Project Overview

**SeeDew** automatically controls the dew heater on a Seestar telescope. It reads temperature and dew point from NINA's weather mediator and turns the heater on/off via Alpaca REST to prevent dew forming on the optics.

## Project Structure

```
NINA.Plugin.SeeDew/
  SeeDewPlugin.cs               ← IPluginManifest + INotifyPropertyChanged; owns DewControlService; options UI bindings
  SeeDewSettings.cs             ← JSON settings persisted to %LOCALAPPDATA%\NINA\SeeDew\settings.json
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

## Key Architecture

**MEF wiring:** `SeeDewPlugin` is exported as both `[Export(typeof(IPluginManifest))]` and `[Export]` so `DewStatusViewModel` can import it by type. `DewStatusViewModel` is exported as `[Export(typeof(IDockableVM))]` and receives `(IProfileService, SeeDewPlugin)` via `[ImportingConstructor]`.

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

## Settings

Persisted at `%LOCALAPPDATA%\NINA\SeeDew\settings.json`.

## Plugin GUID

`1B307E89-2157-4DFB-BFCA-A3ED3A58E272` in `Properties/AssemblyInfo.cs` — never change this after first publish.
