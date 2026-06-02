---
name: run
description: Launch the StreetSamurai Blazor Server host and open it in Chrome at localhost. No arguments needed.
---

# /run — launch the Blazor host + open Chrome

Starts the StreetSamurai Blazor Server app and opens it in Chrome at the local dev URL.

Fixed facts:
- Host project: `v3/StreetSamurai.Blazor`
- Default dev URL: **http://localhost:5101** (the `http` launch profile — no self-signed-cert warning).
  HTTPS profile is `https://localhost:7103` if ever needed.
- Cookie auth: the app loads to a login/landing page; that's expected.

When invoked:

1. **Free the host.** Kill any running instance so the build lock and port 5101 are free:
   ```powershell
   Get-Process StreetSamurai.Blazor -ErrorAction SilentlyContinue | Stop-Process -Force
   ```

2. **Launch the host in the background** (pin the http profile so the URL is deterministic):
   ```
   dotnet run --project v3/StreetSamurai.Blazor --launch-profile http
   ```
   Use `run_in_background: true` — the host is long-lived. If a current build exists, add `--no-build` to start faster.

3. **Wait until it's listening** on 5101 (poll, don't sleep blindly — up to ~90s for a cold build):
   ```powershell
   for ($i=0; $i -lt 90; $i++) {
     if (Get-NetTCPConnection -LocalPort 5101 -State Listen -ErrorAction SilentlyContinue) { "LISTENING"; break }
     Start-Sleep -Seconds 1
   }
   ```

4. **Open Chrome** to the app (fall back to the default browser if Chrome isn't found):
   ```powershell
   $chrome = @(
     "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
     "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
     "$env:LocalAppData\Google\Chrome\Application\chrome.exe"
   ) | Where-Object { Test-Path $_ } | Select-Object -First 1
   if ($chrome) { Start-Process $chrome "http://localhost:5101" }
   else { Start-Process "http://localhost:5101" }
   ```

5. Tell the user it's up at http://localhost:5101 and that the host keeps running in the background
   (stop it with `Get-Process StreetSamurai.Blazor | Stop-Process -Force`).

Notes:
- If port 5101 is already listening when invoked, skip the relaunch and just open Chrome (the host is already up).
- If the build fails, summarize the error; a common cause is a Visual Studio instance holding the DLL lock — close VS or `--no-build` against a known-good build.
