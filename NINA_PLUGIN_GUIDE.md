# NINA Plugin Development Guide

Hard-won lessons from building a NINA 3.x plugin on net8.0-windows. Drop this file into any NINA plugin project for reference.

---

## Project Setup

### Target framework and NuGet

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NINA.Plugin" Version="3.2.0.9001" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>
```

**NuGet source**: `https://api.nuget.org/v3/index.json` only. The old MyGet feed (`myget.org/F/isbeorn`) is dead — remove it from `NuGet.Config` if present or restore will hang/fail.

`NINA.Plugin 3.2.0.9001` pulls in `NINA.Sequencer`, `NINA.WPF.Base`, `NINA.Equipment`, `NINA.Core` etc. as transitive dependencies — you don't need to add them individually unless you need a type only in a specific one.

### Assembly metadata

Keep in `Properties/AssemblyInfo.cs`. The GUID is the stable plugin identity — **never change it after first publish**:

```csharp
[assembly: AssemblyTitle("MyPlugin")]           // must match SeeDew_Options key (see XAML keys below)
[assembly: AssemblyDescription("...")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: Guid("YOUR-GUID-HERE")]              // generate once, never regenerate
```

---

## Installing for Testing

Copy **only** `bin\Debug\net8.0-windows\MyPlugin.dll` to:

```
%LOCALAPPDATA%\NINA\Plugins\3.0.0\<YourPluginFolder>\MyPlugin.dll
```

**Critical**: use the `3.0.0` subfolder, not the NINA version number folder (e.g. `3.2.0.9001`). NINA scans `3.0.0` for plugins regardless of its own running version. No other build output files are needed — NINA ships its own copies of all dependencies.

Restart NINA after copying.

---

## MEF Wiring

NINA uses MEF (Managed Extensibility Framework) for dependency injection throughout.

### Plugin manifest class

```csharp
[Export(typeof(IPluginManifest))]
[Export]                                  // bare Export so other MEF types can import it by concrete type
public class MyPlugin : PluginBase, IPluginManifest, INotifyPropertyChanged {

    [ImportingConstructor]
    public MyPlugin(IWeatherDataMediator weatherMediator) { ... }

    public override async Task Teardown() {
        // clean up background services here
        await base.Teardown();
    }
}
```

The bare `[Export]` (no type parameter) is required so sequencer instructions and dockable VMs can receive it via `[ImportingConstructor]`.

### Dockable panel VM

```csharp
[Export(typeof(IDockableVM))]
public class MyStatusViewModel : BaseVM, IDockableVM {

    [ImportingConstructor]
    public MyStatusViewModel(IProfileService profileService, MyPlugin plugin) { ... }
}
```

### Sequencer instructions

```csharp
[Export(typeof(ISequenceItem))]
[ExportMetadata("Name",        "My Instruction")]
[ExportMetadata("Description", "Does something useful")]
[ExportMetadata("Icon",        "MyPlugin_Icon")]   // key into your ResourceDictionary
[ExportMetadata("Category",    "MyPlugin")]
public class MyInstruction : SequenceItem {

    [ImportingConstructor]
    public MyInstruction(MyPlugin plugin) {
        _plugin = plugin;
        Name = "My Instruction";    // REQUIRED — SequenceBlockView binds to {Binding Name}
                                    // MEF metadata alone is not enough; set it explicitly
    }

    private MyInstruction(MyInstruction clone) : this(clone._plugin) { }

    public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
        await _plugin.MyService.DoThingAsync();
    }

    public override object Clone() => new MyInstruction(this);
}
```

**Always set `Name` in the constructor.** `SequenceBlockView` uses `{Binding Name}` to render the tile label. If `Name` is null the tile appears completely blank. MEF reads `ExportMetadata("Name", ...)` for cataloguing but does not automatically push it onto the `Name` property.

---

## ResourceDictionary

### The file pair

Every plugin needs a `Resources.xaml` + `Resources.xaml.cs` code-behind.

**Resources.xaml**:
```xml
<ResourceDictionary
    x:Class="MyNamespace.Resources"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="clr-namespace:MyNamespace"
    xmlns:vm="clr-namespace:MyNamespace.ViewModels"
    xmlns:views="clr-namespace:MyNamespace.Views"
    xmlns:seq="clr-namespace:MyNamespace.Sequencer"
    xmlns:nina="clr-namespace:NINA.View.Sequencer;assembly=NINA.Sequencer">

    <!-- icon geometry (see Icons section) -->
    <!-- sequencer DataTemplates -->
    <!-- dockable DataTemplate -->
    <!-- options DataTemplate -->

</ResourceDictionary>
```

**Resources.xaml.cs**:
```csharp
using System.ComponentModel.Composition;
using System.Windows;

namespace MyNamespace {
    [Export(typeof(ResourceDictionary))]
    public partial class Resources : ResourceDictionary {
        public Resources() { InitializeComponent(); }
    }
}
```

NINA merges all `[Export(typeof(ResourceDictionary))]` exports into the application resources on startup. Without the code-behind export the plugin's resources (icons, templates) are invisible to NINA.

### XAML template keys — exact format required

| Purpose | Key format | Example |
|---|---|---|
| Options UI | `{AssemblyTitle}_Options` | `SeeDew_Options` |
| Dockable panel | `{FullyQualifiedVMType}_Dockable` | `MyNamespace.ViewModels.MyStatusViewModel_Dockable` |
| Sequencer items | none — use implicit `DataType` only | see below |

```xml
<!-- Options UI — key must exactly match AssemblyTitle + "_Options" -->
<DataTemplate x:Key="SeeDew_Options" DataType="{x:Type local:MyPlugin}">
    <StackPanel> ... </StackPanel>
</DataTemplate>

<!-- Dockable panel — key must be fully qualified VM class name + "_Dockable" -->
<DataTemplate x:Key="MyNamespace.ViewModels.MyStatusViewModel_Dockable"
              DataType="{x:Type vm:MyStatusViewModel}">
    <views:MyStatusView/>
</DataTemplate>

<!-- Sequencer items — NO x:Key, DataType only (implicit lookup) -->
<DataTemplate DataType="{x:Type seq:MyInstruction}">
    <nina:SequenceBlockView/>
</DataTemplate>
```

If a sequencer DataTemplate has an `x:Key` it will never be found by the sequencer's implicit DataType lookup — the item will show blank.

---

## Sequencer Items

### SequenceBlockView is mandatory

Every `ISequenceItem` DataTemplate **must** use `<nina:SequenceBlockView/>` as its root. This control renders:

- The dark tile with icon + name
- Trash bin (delete) button
- Three-dots overflow menu
- "Number of attempts" and "On error" controls
- Up/down reorder arrows
- Drag handle

Without it, items placed in the sequence lack all these controls and cannot be deleted.

**Simple items** (just icon + name, no extra inputs):
```xml
<DataTemplate DataType="{x:Type seq:MyInstruction}">
    <nina:SequenceBlockView/>
</DataTemplate>
```

**Items with extra UI** (e.g. an input field):
```xml
<DataTemplate DataType="{x:Type seq:MyInstruction}">
    <nina:SequenceBlockView>
        <nina:SequenceBlockView.SequenceItemContent>
            <TextBox Text="{Binding MyValue}" Width="80"/>
        </nina:SequenceBlockView.SequenceItemContent>
    </nina:SequenceBlockView>
</DataTemplate>
```

The `SequenceItemContent` area appears in the second row of the block, next to the "Number of attempts" controls.

### The `nina:` xmlns

```xml
xmlns:nina="clr-namespace:NINA.View.Sequencer;assembly=NINA.Sequencer"
```

This resolves against `NINA.Sequencer.dll` which ships with NINA and is available as a transitive NuGet dependency of `NINA.Plugin`. The assembly name is `NINA.Sequencer`, namespace is `NINA.View.Sequencer`.

---

## Icons

### Define your own geometry — never rely on NINA's SVG keys

NINA's built-in icon keys (e.g. `ThermometerSVG`) are not guaranteed to be accessible from plugin ResourceDictionaries and will fail silently. Define your own `GeometryGroup` with a unique key:

```xml
<GeometryGroup x:Key="MyPlugin_Icon" FillRule="Nonzero">
    <PathGeometry Figures="M12,2 L19,12 A7,7 0 1 1 5,12 Z"/>
</GeometryGroup>
```

Reference it in every `ISequenceItem` export:
```csharp
[ExportMetadata("Icon", "MyPlugin_Icon")]
```

### PathGeometry syntax gotcha

`PathGeometry` uses the **`Figures`** attribute for path data, not `Data`:

```xml
<!-- CORRECT -->
<PathGeometry Figures="M12,2 L19,12 A7,7 0 1 1 5,12 Z"/>

<!-- WRONG — MC3072 build error -->
<PathGeometry Data="M12,2 L19,12 A7,7 0 1 1 5,12 Z"/>
```

`Data` is the property on the `Path` UIElement (`<Path Data="..."/>`), not on `PathGeometry`.

---

## Settings Persistence

Persist settings to JSON in `%LOCALAPPDATA%\NINA\Plugins\<YourPlugin>\settings.json` using `Newtonsoft.Json`:

```csharp
public class MyPluginSettings {
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5555;

    private static string Path =>
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NINA", "Plugins", "MyPlugin", "settings.json");

    public static MyPluginSettings Load() {
        try {
            if (File.Exists(Path))
                return JsonConvert.DeserializeObject<MyPluginSettings>(File.ReadAllText(Path))!;
        } catch { }
        return new MyPluginSettings();
    }

    public void Save() {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
```

---

## Options UI

The options DataTemplate binds to the plugin class (`SeeDewPlugin` / `MyPlugin`). Expose bindable properties with `INotifyPropertyChanged` and a save command:

```csharp
public class MyPlugin : PluginBase, IPluginManifest, INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void RaisePropertyChanged([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

    public ICommand SaveSettingsCommand { get; }

    private string _host = "localhost";
    public string Host {
        get => _host;
        set { _host = value; RaisePropertyChanged(); }
    }

    private void ApplyAndSave() {
        Settings.Host = Host;
        Settings.Save();
    }
}
```

The "Save Settings" button pattern (rather than auto-save on every keystroke) avoids mid-edit saves and makes it obvious to the user when settings take effect.

---

## Reacting to Sequence Start / Stop

### Why this matters

A background service started by a sequencer instruction keeps running after the sequence ends unless you explicitly stop it. Hook `ISequenceMediator` to auto-stop when the sequence finishes or is aborted.

### Import the mediator

Add `ISequenceMediator` to the plugin manifest constructor:

```csharp
using NINA.Sequencer.Interfaces.Mediator;

[ImportingConstructor]
public MyPlugin(IWeatherDataMediator weatherMediator, ISequenceMediator sequenceMediator) {
    _sequenceMediator = sequenceMediator;
    _sequenceMediator.SequenceStarting += OnSequenceStarting;
    _sequenceMediator.SequenceFinished += OnSequenceFinished;
    ...
}
```

### Handler signature — must return `Task`

`SequenceFinished` and `SequenceStarting` use an async delegate, **not** `EventHandler`. Handlers must return `Task` or the compiler emits CS0407 "wrong return type":

```csharp
private Task OnSequenceStarting(object? sender, EventArgs e) => Task.CompletedTask;

private async Task OnSequenceFinished(object? sender, EventArgs e) {
    await MyService.StopAsync();
}
```

`async void` will compile but swallows exceptions — always use `async Task`.

### Unsubscribe in Teardown

```csharp
public override async Task Teardown() {
    _sequenceMediator.SequenceStarting -= OnSequenceStarting;
    _sequenceMediator.SequenceFinished -= OnSequenceFinished;
    await MyService.StopAsync();   // also stop on NINA close
    await base.Teardown();
}
```

Unsubscribing prevents a double-stop if NINA closes while the sequence is still running.

### Process lifetime summary

| Event | What happens |
|---|---|
| Sequence finishes / stopped | `SequenceFinished` fires → `StopAsync()` called |
| NINA closed | `Teardown()` called → handlers unsubscribed → `StopAsync()` called |
| Process dies | NINA's process exits, taking all plugin threads with it — no orphan processes |

---

## Common Pitfalls Summary

| Symptom | Cause | Fix |
|---|---|---|
| Sequencer items blank in sidebar | DataTemplate has `x:Key` | Remove `x:Key`, use `DataType` only |
| Sequencer items lack delete/three-dots | DataTemplate doesn't use `SequenceBlockView` | Wrap template in `<nina:SequenceBlockView/>` |
| Sequencer tile shows blank (name missing) | `Name` not set on instruction | Add `Name = "..."` in constructor |
| Icon missing / blank in tile | Relying on NINA's built-in SVG key | Define own `GeometryGroup` in ResourceDictionary |
| Build error MC3072 on PathGeometry | Used `.Data` attribute | Use `.Figures` attribute instead |
| Plugin options show unexpected toggles | `PluginBase` has default `AutoStart`/`ManageConnection` properties | Remove them from plugin class and any base class binding |
| NuGet restore fails/hangs | MyGet feed in `NuGet.Config` | Replace with `https://api.nuget.org/v3/index.json` |
| Plugin not loaded by NINA | DLL in wrong folder | Must be in `3.0.0\<Name>\`, not the NINA version folder |
| ResourceDictionary resources not found | Missing `[Export(typeof(ResourceDictionary))]` code-behind | Add `Resources.xaml.cs` with the export |
