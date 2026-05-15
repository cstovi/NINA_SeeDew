# CLAUDE.md

## Project Overview

**SeeDew** automatically controls the dew heater on a Seestar telescope. It reads temperature and dew point from NINA's weather mediator and turns the heater on/off via NINA's switch mediator to prevent dew forming on the optics.

## Project Structure

```
NINA.Plugin.SeeDew/
  SeeDewPlugin.cs               ← IPluginManifest + INotifyPropertyChanged; owns DewControlService; options UI bindings
  SeeDewSettings.cs             ← JSON settings persisted to %LOCALAPPDATA%\NINA\SeeDew\settings.json
  Services/
    DewControlService.cs        ← polling loop, hysteresis logic, events (CycleCompleted, LogEntryAdded, StatusChanged)
  Sequencer/
    StartDewControlInstruction.cs ← sequencer instruction to start automatic control
    StopDewControlInstruction.cs  ← sequencer instruction to stop automatic control
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

**Device access:** heater state is read/written through `ISwitchMediator` (switch `Id=0`), and weather comes from `IWeatherDataMediator`.

## Settings

Persisted at `%LOCALAPPDATA%\NINA\SeeDew\settings.json`.

## Plugin GUID

`1B307E89-2157-4DFB-BFCA-A3ED3A58E272` in `Properties/AssemblyInfo.cs` — never change this after first publish.
