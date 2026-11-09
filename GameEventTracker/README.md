# Airi's Desktop

This is the native Windows WPF build. It runs in its own window and does not start a web server or open a browser.

Double-click `Run-Airis-Desktop.bat` to launch it. The published executable and its self-contained runtime are in `AiriDesktop\bin\Release\net8.0-windows\win-x64\publish`.

Progress is stored locally in `%LOCALAPPDATA%\AiriDesktop\state.json`; imported portraits are in `%LOCALAPPDATA%\AiriDesktop\portraits`. Back up both folders to preserve the account. The older HTML files remain in the project as an unused backup; the desktop app has its own separate save data.

## Recommendations

- Keep JSON storage while this remains a single-user app; add a visible export/import backup flow before adding more data.
- Consider SQLite if you add searchable event history, many inventory records, or multiple profiles.
- Use a signed MSIX installer for updates and Start Menu integration if you distribute the desktop build later.