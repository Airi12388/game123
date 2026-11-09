# Airi's Desktop

A native Windows WPF application. It does not start a web server, open a browser, or require an internet connection to run.

## Run from source

Install the .NET 8 SDK, then run:

```powershell
dotnet run --project .\AiriDesktop.csproj
```

## Publish a standalone Windows build

```powershell
dotnet publish .\AiriDesktop.csproj -c Release -r win-x64 --self-contained true
```

The executable is written to `bin\Release\net8.0-windows\win-x64\publish\AiriDesktop.exe`.

## Local data

Account data is saved at `%LOCALAPPDATA%\AiriDesktop\state.json`. Portrait files selected in the app are copied to `%LOCALAPPDATA%\AiriDesktop\portraits`. Back up both locations to preserve progress and custom portraits.

The existing files in the parent folder are the earlier browser version; the WPF app stores its account separately and does not migrate browser `localStorage` automatically.