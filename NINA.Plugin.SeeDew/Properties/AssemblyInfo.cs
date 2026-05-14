using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("SeeDew")]
[assembly: AssemblyDescription("Automatic dew heater control for Seestar telescope")]
[assembly: AssemblyCompany("Carl Stovell")]
[assembly: AssemblyProduct("SeeDew")]
[assembly: AssemblyCopyright("Copyright © 2026 Carl Stovell")]
[assembly: Guid("1B307E89-2157-4DFB-BFCA-A3ED3A58E272")]
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]

[assembly: AssemblyMetadata("License", "MIT")]
[assembly: AssemblyMetadata("LicenseURL", "https://opensource.org/licenses/MIT")]
[assembly: AssemblyMetadata("Repository", "https://github.com/cstovi/NINA_SeeDew")]
[assembly: AssemblyMetadata("Tags", "Seestar,dew,heater,weather,automation")]
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.1001")]
[assembly: AssemblyMetadata("ShortDescription", "Automatically turns the Seestar dew heater on and off based on temperature/dew point margin.")]
[assembly: AssemblyMetadata("LongDescription", @"SeeDew monitors the dew point margin (temperature minus dew point) using NINA's connected weather device and automatically controls the Seestar's dew heater via its Alpaca REST API.

The heater turns ON when the margin drops below a configurable threshold (default 3.5°C) and turns OFF when it rises above another threshold (default 5.0°C), preventing dew and frost on the optics without manual intervention.

Features:
- Integrates with any weather device already connected in NINA
- Direct Alpaca control of the Seestar heater switch — no extra device setup required
- Live status panel in the imaging tab showing temperature, dew point, margin, and heater state
- Optional Discord webhook notifications on heater state changes (log is always written to %LOCALAPPDATA%\NINA\SeeDew\seedew.log)
- Auto-starts with NINA; cleanly shuts the heater off when NINA closes

Thanks to @Astrowook for ideas, skills and laughs")]
[assembly: AssemblyMetadata("FeaturedImageURL", "https://i.ibb.co/jkMX7y6m/See-Dew.png")]
[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/cstovi/NINA_SeeDew/releases")]
