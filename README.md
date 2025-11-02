# Shutdown Schedule

Hi there! 👋 This WinForms project is my personal shortcut for taming Windows shutdowns. I found myself jumping between command prompts, Task Scheduler, and random scripts just to time a shutdown, so I built a tool that keeps everything one click away.

## What This App Does

- Schedule a shutdown for any future date/time with a clean UI (separate date + time pickers).
- Cancel scheduled shutdowns after authenticating with a password I set.
- Trigger immediate shutdown, restart, log-off, or hibernate commands from a toolbar or the system tray.
- Keep running in the tray with balloon notifications so I can tuck it away after scheduling.
- Toggle between light and dark themes, including the title bar and the DateTimePicker drop-down calendar.
- Record an activity log so I can see what commands fired and when.

## Tech Stack & Choices

- **.NET 8.0 (Windows)** with WinForms for quick desktop UI work.
- P/Invoke to **DWM**, **Uxtheme**, and **User32/GDI** for true dark-mode support on the title bar and pickers.
- **PBKDF2** (via `Rfc2898DeriveBytes`) to hash/salt the cancel password and store it in `%APPDATA%\ShutdownSchedule`.
- Async `ProcessStartInfo` calls to run the built-in `shutdown.exe` commands without freezing the UI.
- Tray integration using `NotifyIcon` so it feels native.

## What I Learned Building It

- How to wrestle WinForms controls into modern-ish shapes, including owner-drawing the DateTimePicker edit fields for dark mode.
- Persisting settings + logs safely under AppData while avoiding cross-thread GUI issues.
- Balancing UI design and Windows security (UAC elevation via manifest, password protection around cancellations).
- General ergonomics of WinForms layout: panels, flow layouts, and on-the-fly dialog generation with shared theming.

## Run It Yourself

### Visual Studio

1. Install Visual Studio 2022 17.8+ with the “.NET desktop development” workload (must include .NET 8.0 SDK).
2. Open `ShutdownSchedule.sln`.
3. Restore packages if prompted.
4. Build and run (`Ctrl+Shift+B`, then `F5`/`Ctrl+F5`). Accept the UAC prompt—admin rights are required for shutdown commands.

### Command Line

```bash
cd path/to/ShutdownSchedule
dotnet build
```

Launch `bin/Debug/net8.0-windows/ShutdownSchedule.exe` (or the Release build if you prefer) and approve the UAC prompt.

## Things Left To Polish / Future Ideas

- Package it as a single-file installer (MSIX or self-contained publish) to simplify deployment.
- Add localization + configurable hotkeys for power users.
- Surface the activity log directly in the tray tooltip and export it to CSV.
- Optional countdown visible right on the tray icon.
- Maybe swap WinForms for WinUI 3 if I feel like a bigger modernization challenge someday.

## Personal Notes

This project was built for my daily workflow, but I’m sharing it in case someone else wants a reliable, familiar UI for scheduled shutdowns. Feel free to fork and tweak—it’s intentionally simple so new contributors can jump in.

If you’ve read this far, thanks! Drop an issue or suggestion if you try it out.
