# AGENTS.md

## Project Overview

OpenCode Cost Meter is a cross-platform Qt desktop widget that displays today's OpenCode LLM spend in real time, broken down by model. It reads the OpenCode SQLite database directly in read-only mode.

## Tech Stack

- Qt 6.8+ Widgets, SQL, and Test
- C++20
- CMake 3.25+
- Qt `QSystemTrayIcon` for tray integration
- Qt SQLite driver for database access

## Active Project

The only active application is under `qt/`. Do not reintroduce the removed WPF/.NET implementation or Windows-only APIs unless explicitly requested.

```text
qt/
├─ resources/                    # Icon, fonts, notices, and display-name rules
├─ src/models.h                  # Shared value types
├─ src/services.*                # Database, polling, settings, and formatting
├─ src/widget_window.*           # Frameless widget and interactions
├─ src/main.cpp                  # Application startup and tray lifecycle
├─ tests/                        # Qt Test coverage
└─ CMakeLists.txt
```

## Database

The application reads `~/.local/share/opencode/opencode.db` by default, or a path supplied with `--db-path`. It uses the `message` table rather than `session` because session totals are cumulative. Assistant messages are filtered by `$.time.completed` and forked messages are deduplicated by created/completed timestamps before aggregation.

Database access must remain read-only and off the GUI thread. Preserve the existing SQL accounting semantics when changing the repository.

## Build And Test

From a Visual Studio Developer PowerShell on Windows:

```powershell
cmake -S qt -B build/qt -DCMAKE_PREFIX_PATH="C:\Qt\6.11.1\msvc2022_64"
cmake --build build/qt
ctest --test-dir build/qt --output-on-failure
```

The `build/` directory is ignored by Git. On other platforms, use the installed Qt kit in `CMAKE_PREFIX_PATH`.

## Behavior To Preserve

- Initial immediate poll, configurable polling interval, and no overlapping database queries.
- Two-second spend highlighting and last-known values during refreshes/errors.
- Frameless always-on-top widget, drag-to-move, hide-on-close, tray restore, and explicit Exit.
- Context menu controls for topmost, polling interval, opacity, centering, hide, and exit.
- Settings debounce and persisted position/opacity/interval/expanded state.
- Working-area clamping and quadrant-based resize anchoring where the platform permits programmatic positioning.
- Cascadia Mono for numeric/model text and Inter for UI text. Do not bundle Segoe UI.

## Platform Notes

Windows, macOS, and Linux X11 are supported targets. Wayland compositors may restrict absolute positioning, translucency, always-on-top, or tray behavior; do not claim stronger guarantees than the compositor provides.

## Command-Line Options

- `--db-path <path>` uses an alternative OpenCode database.
- `--help` prints usage and exits.
