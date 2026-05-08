# NINA SeeDew Plugin

SeeDew automatically controls the dew heater on a Seestar telescope using weather data already connected in NINA.

It monitors the dew point margin (`temperature - dew point`) and applies hysteresis to avoid rapid toggling:

- Heater turns ON when margin is below the ON threshold
- Heater turns OFF when margin is above the OFF threshold

## Version

Current plugin version: `1.2.0.0`

## Features

- Automatic dew heater control using NINA weather and switch mediators
- Configurable ON/OFF thresholds and poll interval
- Dockable status panel with live readings, heater state, and service log
- Sequencer instructions:
  - `SeeDew Start Dew Control`
  - `SeeDew Stop Dew Control`
- Optional Discord webhook notifications for start/stop and state changes
- Session log files written under `%LOCALAPPDATA%\NINA\SeeDew`

## Requirements

- NINA 3.x (minimum application version `3.0.0.1001`)
- A connected weather source in NINA (temperature and dew point)
- Seestar switch connected in NINA Switch panel

## Install

Since SeeDew is not currently in the NINA plugin repository, install it manually:

1. Create this folder if it does not exist:
   - `%LOCALAPPDATA%\NINA\Plugins\3.0.0\SeeDew\`
2. Drop `NINA.Plugin.SeeDew.dll` into that folder.
3. Restart NINA.

## Usage

1. Connect your weather source and Seestar switch in NINA.
2. In plugin options, set thresholds/poll interval — changes are saved automatically.
3. Add sequence items:
   - `SeeDew Start Dew Control` after device connections
   - `SeeDew Stop Dew Control` before disconnections
4. Optionally monitor the dockable SeeDew status panel during runs.

## Settings

Settings are persisted automatically to:

- `%LOCALAPPDATA%\NINA\SeeDew\settings.json`

Sequencer instructions (`SeeDew Start/Stop Dew Control`) reload this file before they run, so option changes are picked up on the next sequence execution without restarting NINA.

## Notes

- Plugin identity GUID is stable and must not be changed after publish.
- Existing dependency warnings (for some transitive packages) may appear at build time, but current Release builds succeed.
