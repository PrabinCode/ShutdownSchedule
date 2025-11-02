# Shutdown Schedule

## Build and Run (Visual Studio)

1. Open `ShutdownSchedule.sln` in Visual Studio 2022 (17.8+) with the .NET 8.0 desktop workload installed.
2. When prompted, restore NuGet packages or use **Build > Restore NuGet Packages**.
3. Build the solution with **Build > Build Solution** (or press `Ctrl+Shift+B`).
4. Run the app with **Debug > Start Without Debugging** (`Ctrl+F5`) or **Start Debugging** (`F5`). UAC will prompt for administrator approval on first launch due to the manifest.

## Build from Command Line

```bash
cd "e:/Fun Projects/ShutdownSchedule"
dotnet build
```

Run the resulting executable at `bin/Debug/net8.0-windows/ShutdownSchedule.exe` to launch the app if you prefer the command line workflow.

## Notes

- The application manifest (`app.manifest`) requests elevation, so Windows will show a UAC dialog the first time you start the app.
- Logs and settings are stored under `%APPDATA%\ShutdownSchedule`.
- The project targets .NET 8.0 (preview); ensure the matching SDK/runtime is installed.
