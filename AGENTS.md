# AGENTS.md

## Project Overview

OpenCode Cost Meter is a cross-platform Qt Widgets desktop widget that displays today's OpenCode LLM spend in real time, broken down by provider/model. It reads the OpenCode SQLite database directly in read-only mode.

## Tech Stack

- Qt 6.8+ (`Core`, `Gui`, `Widgets`, `Sql`, and `Test`)
- C++20
- CMake 3.25+
- Qt `QSystemTrayIcon` for tray integration
- Qt `QSqlDatabase` with the `QSQLITE` driver

## Active Project

The only active application is under `qt/`. Do not reintroduce the removed WPF/.NET implementation or Windows-only APIs unless explicitly requested.

```text
qt/
├─ resources/
│  ├─ app.qrc                   # Icon and bundled license notices
│  ├─ licenses/                 # Cascadia and Inter OFL notices
│  └─ model-display-names.txt   # Editable display-name rules
├─ src/
│  ├─ main.cpp                  # Startup, argument parsing, and tray lifecycle
│  ├─ models.h                  # Settings and usage value types
│  ├─ services.h/.cpp            # Database, polling, settings, and formatting
│  └─ widget_window.h/.cpp       # Frameless widget, rows, menu, and interactions
├─ tests/repository_tests.cpp   # Qt Test SQLite aggregation fixture
└─ CMakeLists.txt
publish-windows.ps1             # Windows release build and ZIP packaging
```

Fonts are downloaded or copied into the build directory during CMake configuration and embedded into the executable as Qt resources. They are not source files under `qt/resources`.

## Database

The default database path is always `~/.local/share/opencode/opencode.db`, including on Windows. `--db-path` overrides it. `DbLocator` only returns paths that already exist; a missing database causes a critical message box and exit code 1.

`MessageTableRepository` uses the `message` table rather than `session` because session totals are cumulative. The query:

- Filters for assistant messages.
- Filters by non-null `$.time.completed` at or after local midnight.
- Extracts provider, model, and cost with SQLite JSON functions.
- Deduplicates forked messages by grouping on created/completed timestamps.
- Aggregates by provider/model and orders by cost descending, then model ascending.

Database access must remain read-only and off the GUI thread. `UsageWorker` owns the repository and runs in a dedicated `QThread`; `UsagePoller` uses queued signals and prevents overlapping queries with `m_inFlight`. Preserve the SQL accounting semantics when changing the repository.

The SQLite connection uses `QSQLITE_OPEN_READONLY` and a 2-second busy timeout. The packaged build must include the Qt `sqldrivers/qsqlite.dll` plugin on Windows.

## Services And State

- `UsagePoller` performs an immediate first poll, then polls on a configurable timer. The interval is clamped to at least 250 ms.
- `SettingsStore` writes JSON to `QStandardPaths::AppConfigLocation/OpenCodeCostMeter.settings.json`. It also reads the old adjacent-to-executable settings file once as a migration fallback.
- Settings fields are `x`, `y`, `alwaysOnTop`, `pollIntervalSeconds`, `opacity`, and `isExpanded`.
- Settings writes are debounced by 500 ms after movement, menu slider changes, centering, or expansion changes.
- `ModelDisplayNameRules` loads `model-display-names.txt` beside the executable once, applies default hyphen/title formatting, then prefix replacement rules, and caches results.

## UI And Interaction Behavior

- `WidgetWindow` is a frameless `Qt::Tool` window with optional `WindowStaysOnTopHint`, translucent background, dark card styling, and no taskbar entry.
- The window remains hidden until the first successful poll or first error. Query errors replace the normal content with an error message; they do not leave the normal total visible.
- Total cost is formatted as US currency with two decimals. Model rows use three decimals and hide values below `$0.0005`.
- Changed total/model values are highlighted for two seconds. Existing rows are reused when possible, but current row ordering follows insertion order rather than an explicit cost-order diff in the widget layer.
- A click toggles expansion; dragging past Qt's drag threshold moves the widget instead. Right-click opens the context menu.
- The context menu contains Always on top, poll interval, opacity, horizontal/vertical centering, Hide, and Exit. Keyboard shortcuts are `A`, `H`, `V`, and `T`.
- Closing hides the widget unless Exit was explicitly selected. The tray icon toggles visibility when a system tray is available. If no tray is available, the widget context menu remains the exit path.
- Position clamping uses `QScreen::availableGeometry()` after dragging and first display. Resize compensation anchors the current right/bottom side when the widget is there; this is platform best effort, not a full WPF quadrant/span implementation.

## Fonts And Resources

- Cascadia Mono is used for numeric/model labels and is bundled under SIL OFL 1.1.
- Inter is used for menu/slider controls and is bundled under SIL OFL 1.1.
- Segoe UI is not bundled because it is not generally redistributable with this application.
- `model-display-names.txt` is deliberately installed beside the executable so users can edit display rules without rebuilding.
- Keep `THIRD-PARTY-NOTICES.md` and the embedded license resources synchronized with bundled fonts and Qt distribution obligations.

## Build And Test

For a normal configured Qt build:

```powershell
cmake -S qt -B build/qt -DCMAKE_PREFIX_PATH="C:\Qt\6.11.1\msvc2022_64"
cmake --build build/qt
ctest --test-dir build/qt --output-on-failure
```

The `build/` directory and generated `OpenCodeCostMeter.zip` are ignored by Git. CMake downloads Inter and, when Cascadia Mono is not installed on Windows, Cascadia Code during configuration. A working network connection is therefore required for a clean font build unless those inputs are already cached in the build directory.

## Windows Publishing

Run this from any PowerShell at the repository root:

```powershell
.\publish-windows.ps1
```

The script locates Visual Studio and imports the x64 MSVC environment, configures a Release NMake build, runs CTest, invokes `windeployqt`, prunes unused Qt plugins, copies app-local MSVC runtime DLLs when available, and creates `OpenCodeCostMeter.zip` in the repository root. The default Qt bin directory is `C:\Qt\6.11.1\msvc2022_64\bin`; use `-QtBin` to override it.

The resulting ZIP is a portable multi-file deployment. It contains the executable, Qt DLLs, `platforms/qwindows.dll`, `sqldrivers/qsqlite.dll`, required image/icon/style plugins, MSVC runtime DLLs, the model rules, and license notices. A single executable is not produced because this project uses dynamically linked Qt.

## Platform Notes

Windows, macOS, and Linux X11 are supported targets. Wayland compositors may restrict absolute positioning, translucency, always-on-top, or tray behavior. The code does not provide a separate Wayland fallback or runtime capability report; do not claim stronger guarantees than the compositor provides.

## Command-Line Options

- `--db-path <path>` uses an alternative OpenCode database.
- `--help` invokes Qt's built-in help handling and exits.
