# OpenCode Cost Meter

A borderless desktop widget that shows your [opencode](https://opencode.ai) LLM spend for today in real time, broken down by model. The Qt implementation supports Windows, macOS, and Linux X11. Wayland compositors may restrict widget positioning, always-on-top behavior, and tray interaction.

## Usage

Requires Qt 6.8+ with the Widgets, SQL, and Test modules, CMake 3.25+, and a C++20 compiler.

On Windows, open a **Visual Studio Developer PowerShell** and run:

```powershell
cmake -S qt -B build/qt -DCMAKE_PREFIX_PATH="C:\Qt\6.11.1\msvc2022_64"
cmake --build build/qt
.\build\qt\OpenCodeCostMeter.exe
```

The widget stays hidden until its first successful database poll or error. On other platforms, replace the Qt path with the installed Qt kit:

```sh
cmake -S qt -B build/qt -DCMAKE_PREFIX_PATH=/path/to/Qt/6.x/<kit>
cmake --build build/qt
./build/qt/OpenCodeCostMeter
```

By default the widget reads from `~/.local/share/opencode/opencode.db`. To use a different database path:

```powershell
.\build\qt\OpenCodeCostMeter.exe --db-path "C:\path\to\opencode.db"
```

Settings are stored as `OpenCodeCostMeter.settings.json` in the platform application-config directory. The model display-name rules file remains next to the executable.

The application embeds Cascadia Mono (SIL OFL 1.1) and Inter (SIL OFL 1.1). Segoe UI is not bundled because Microsoft does not grant general font redistribution rights with Windows. See `THIRD-PARTY-NOTICES.md`.

The active implementation is the Qt application under `qt/`. The former Windows-only WPF/.NET implementation has been removed.

## Publish For Windows

The recommended Windows distribution is a self-contained portable folder or ZIP archive. Qt is dynamically linked, so a true single-file `.exe` is not produced by the normal build. The application does not require an installer, registry entries, or a separate font installation.

Build the release executable from a **Visual Studio Developer PowerShell**:

```powershell
cmake -S qt -B build/qt -DCMAKE_PREFIX_PATH="C:\Qt\6.11.1\msvc2022_64" -DCMAKE_BUILD_TYPE=Release
cmake --build build/qt --config Release
```

Deploy Qt and the MSVC runtime into a distribution directory. Adjust `$qtBin` if Qt is installed elsewhere:

```powershell
$qtBin = "C:\Qt\6.11.1\msvc2022_64\bin"
$dist = Join-Path $PWD "dist\OpenCodeCostMeter"

Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item $dist -ItemType Directory | Out-Null

& (Join-Path $qtBin "windeployqt.exe") `
    --release `
    --compiler-runtime `
    --no-translations `
    --dir $dist `
    (Join-Path $PWD "build\qt\OpenCodeCostMeter.exe")

Copy-Item "build\qt\model-display-names.txt" $dist
Copy-Item "LICENSE", "THIRD-PARTY-NOTICES.md" $dist
```

Test the published copy before sharing it:

```powershell
& .\dist\OpenCodeCostMeter\OpenCodeCostMeter.exe --help
& .\dist\OpenCodeCostMeter\OpenCodeCostMeter.exe --db-path "C:\path\to\opencode.db"
```

The resulting directory contains the application executable, Qt DLLs, the `platforms\qwindows.dll` platform plugin, the `sqldrivers\qsqlite.dll` SQLite plugin, and the MSVC runtime DLLs. Cascadia Mono and Inter are embedded in the executable resources, so their font files do not need to be distributed separately. Keep `model-display-names.txt` beside the executable because it is intentionally user-editable.

Create a ZIP archive for distribution:

```powershell
Compress-Archive `
    -Path ".\dist\OpenCodeCostMeter\*" `
    -DestinationPath ".\dist\OpenCodeCostMeter-win-x64.zip" `
    -Force
```

Users can extract the ZIP and launch `OpenCodeCostMeter.exe`. Windows Defender SmartScreen may show a warning for unsigned binaries. Code-signing the executable and archive with a trusted certificate is recommended for public distribution.

### Single-File Executable

A single `.exe` would require building Qt statically and statically linking the Qt libraries and plugins. That is not a normal Qt kit feature and introduces additional LGPL/compliance obligations. It is therefore not the recommended distribution route. The portable ZIP above is the smallest practical self-contained package for the current dynamically linked Qt build.
