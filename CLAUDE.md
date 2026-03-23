# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run

# Publish single-file self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

There are no automated tests in this project.

## Architecture

**VolleyStatsPro** is a .NET 8.0 WPF volleyball statistics desktop app (Windows only) using SQLite for persistence.

### Layer overview

| Layer | Path | Responsibility |
|---|---|---|
| Entry point | `App.xaml` / `App.xaml.cs` | App startup, global resources |
| Shell | `MainWindow.xaml` / `MainWindow.xaml.cs` | Top header, tab navigation, view switching |
| Views | `Views/` | `UserControl`-based panels, one per tab |
| Data | `Data/Database.cs` | SQLite schema, repository classes, `StatsService` |
| Models | `Models/Models.cs` | All entity and stats DTOs |
| Controls | `Controls/` | Custom WPF `FrameworkElement` chart and heatmap controls (drawn via `DrawingContext`) |
| Theme | `Helpers/Theme.cs` | Color palette, fonts, shared drawing helpers |

### Data flow

`MainWindow` hosts all views and switches between them. Views call repository/service methods from `Database.cs` directly. There is no DI container — dependencies are passed manually or instantiated inline.

### Database

SQLite file lives at `%APPDATA%\VolleyStatsPro\volleystats.db`, auto-created on first run. All schema and migrations are in `Database.cs`.

### Action coding

Volleyball actions follow DataVolley conventions. Result codes: `#` (perfect), `+` (positive), `!` (overpass), `-` (negative), `/` (error), `=` (blocked/reception error). Court is divided into 9 zones matching standard volleyball position numbering.

### Key classes

- `StatsService` — aggregates raw `Action` rows into `PlayerStats` / `TeamStats` / `ZoneData`.
- `CourtHeatmapControl` — WPF `FrameworkElement`; renders a full-court 9-zone heatmap via `DrawingContext`. Accepts separate `ZoneData` lists for home (bottom half) and away (top half) sides.
- `LiveMatchView` — the most complex view; manages active set/rally state, emits `Action` records on each button click. Contains:
  - DataVolley-style terminal console with command history
  - Live court heatmap (both sides)
  - Rotation bar above the terminal showing home and away 2×3 grids (away grid is mirrored to match on-court orientation)
  - Sliding player drawer (animates in from the right, lists both rosters)
  - Zone pre-selection buttons and match control buttons (point / end set / end match)
- `Theme` — static class; all views use `Theme.Colors.*` and `Theme.Fonts.*` for consistent styling.