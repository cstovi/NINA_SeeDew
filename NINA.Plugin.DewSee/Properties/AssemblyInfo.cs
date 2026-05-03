using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("DewSee")]
[assembly: AssemblyDescription("Automatic dew heater control for Seestar telescope")]
[assembly: AssemblyCompany("Carl Stovell")]
[assembly: AssemblyProduct("DewSee")]
[assembly: AssemblyCopyright("Copyright © 2026 Carl Stovell")]
[assembly: Guid("1B307E89-2157-4DFB-BFCA-A3ED3A58E272")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: AssemblyMetadata("License", "MIT")]
[assembly: AssemblyMetadata("LicenseURL", "https://opensource.org/licenses/MIT")]
[assembly: AssemblyMetadata("Repository", "https://github.com/cstovi/DewSee")]
[assembly: AssemblyMetadata("Tags", "Seestar,dew,heater,weather,automation")]
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.1001")]
[assembly: AssemblyMetadata("ShortDescription", "Automatically turns the Seestar dew heater on and off based on temperature/dew point margin.")]
[assembly: AssemblyMetadata("LongDescription", @"DewSee monitors the dew point margin (temperature minus dew point) using NINA's connected weather device and automatically controls the Seestar's dew heater via its Alpaca REST API.

The heater turns ON when the margin drops below a configurable threshold (default 3.5°C) and turns OFF when it rises above another threshold (default 5.0°C), preventing dew and frost on the optics without manual intervention.

Features:
- Integrates with any weather device already connected in NINA
- Direct Alpaca control of the Seestar heater switch — no extra device setup required
- Live status panel in the imaging tab showing temperature, dew point, margin, and heater state
- Optional Discord webhook notifications on heater state changes
- Auto-starts with NINA; cleanly shuts the heater off when NINA closes")]
[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/cstovi/DewSee/releases")]
