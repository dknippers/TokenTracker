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

The recommended Windows distribution is a self-contained portable ZIP archive. Qt is dynamically linked, so a true single-file `.exe` is not produced by the normal build. The application does not require an installer, registry entries, or a separate font installation.

From any PowerShell at the repository root, run:

```powershell
.\publish-windows.ps1
```

The script configures a Release build, compiles and tests it, runs `windeployqt`, bundles the Qt platform and SQLite plugins plus the MSVC runtime, and creates `OpenCodeCostMeter.zip` in the repository root. It also includes `model-display-names.txt`, `LICENSE`, and `THIRD-PARTY-NOTICES.md`.

The script automatically locates Visual Studio and imports the MSVC build environment. No separate Developer PowerShell setup is required.

If Qt is installed somewhere else, pass its `bin` directory:

```powershell
.\publish-windows.ps1 -QtBin "D:\Qt\6.11.1\msvc2022_64\bin"
```

Test the published copy by extracting the ZIP and running:

```powershell
& .\OpenCodeCostMeter\OpenCodeCostMeter.exe --help
& .\OpenCodeCostMeter\OpenCodeCostMeter.exe --db-path "C:\path\to\opencode.db"
```

Cascadia Mono and Inter are embedded in the executable resources, so their font files do not need to be distributed separately. Keep `model-display-names.txt` beside the executable because it is intentionally user-editable.

The script stages intermediate files under `build\windows-publish`. The resulting ZIP contains the application executable, Qt DLLs, the `platforms\qwindows.dll` platform plugin, the `sqldrivers\qsqlite.dll` SQLite plugin, and the MSVC runtime DLLs.

Users can extract the ZIP and launch `OpenCodeCostMeter.exe`. Windows Defender SmartScreen may show a warning for unsigned binaries. Code-signing the executable and archive with a trusted certificate is recommended for public distribution.

### Single-File Executable

A single `.exe` would require building Qt statically and statically linking the Qt libraries and plugins. That is not a normal Qt kit feature and introduces additional LGPL/compliance obligations. It is therefore not the recommended distribution route. The portable ZIP above is the smallest practical self-contained package for the current dynamically linked Qt build.
