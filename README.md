# Killit Desktop Device Owner Authenticator

A Windows program (Windows 8, 8.1, 10, 11) that makes the [Killit](https://tamodak.org) app
(`org.tamodak.killit`) the **Device Owner** of an Android phone over USB — the
`adb shell dpm set-device-owner` step, without a terminal.

## How it works

1. **Connected phones** — on start and on *Refresh* the program lists every phone adb sees, with its
   state (ready, waiting for the "Allow USB debugging?" prompt, offline).
2. **Check** — for the selected phone it verifies that Killit is installed (otherwise it links to
   tamodak.org), that the phone has no Device Owner yet (otherwise it must be factory reset), and
   lists everything Android would refuse provisioning for: signed-in **accounts** and **extra users /
   work profiles**. The user removes them on the phone and presses *Check* again.
3. **Authenticate** — enabled only once the list is empty. Runs
   `dpm set-device-owner org.tamodak.killit/.admin.KillitDeviceAdminReceiver`, then confirms the
   result with `dpm list-owners` (Android 12+) or `dumpsys device_policy`. Failures show the
   phone's message plus a hint for the known cases (accounts, users, existing owner, Xiaomi's
   "USB debugging (Security settings)").

adb cannot remove accounts on a non-rooted phone, so removal is always done by the user.

The UI follows the Windows display language: English, Indonesian, German, Spanish, French,
Portuguese, Turkish, Russian and Arabic (right-to-left) — the languages Killit itself supports.

## Compatibility choices

| Component | Version | Why |
|---|---|---|
| .NET Framework | 4.5 | Built into Windows 8 / 8.1, covered by 4.6+ in Windows 10 / 11: nothing to install. |
| adb (platform-tools) | r34.0.5 | Newer adb imports `ProcessPrng` (bcryptprimitives.dll), which only exists on Windows 10+. |
| Universal C Runtime | KB2999226 | adb needs it. Windows 10 / 11 have it; the installer adds it on Windows 8 / 8.1 if missing. |
| Google USB Driver | r13 | Optional setup task; Windows 8 does not fetch ADB drivers by itself. |
| Inno Setup | 6.7.3 | Setup runs on Windows 7 and later. |

Every download is pinned by SHA-256 in `build.sh`.

## Building (Linux)

```bash
sudo apt install dotnet-sdk-8.0 wine wine64 wine32:i386   # once; needs i386 enabled
./build.sh
```

The script downloads and verifies the dependencies (cached in `build/deps`), runs the unit tests,
builds the program, installs Inno Setup into a private Wine prefix (`build/wine`) and writes
`dist/KillitDesktopAuthenticator-Setup-<version>.exe`. The version comes from `<Version>` in
`src/KillitDesktopAuthenticator/KillitDesktopAuthenticator.csproj`.

Run just the tests: `dotnet test tests/KillitDesktopAuthenticator.Tests`

## Layout

```
src/KillitDesktopAuthenticator/
  Adb/AdbClient.cs      runs the bundled adb.exe
  Adb/AdbParsers.cs     parses adb / dumpsys / pm / dpm output (unit tested)
  Adb/Provisioner.cs    Check and Authenticate workflow
  MainForm.cs           the window
  Properties/Strings*.resx   UI text, one file per language
installer/KillitDesktopAuthenticator.iss   Inno Setup script
tests/KillitDesktopAuthenticator.Tests/    xunit tests (net8.0, run on Linux)
```

## Testing on Windows

Not verifiable from Linux, so worth checking on real machines:

- [ ] Windows 8.0 and 8.1 without KB2999226: setup installs the update, adb starts afterwards.
- [ ] Windows 8: the Google USB driver task installs, a Pixel/Nexus phone shows up.
- [ ] Windows 10 / 11: install, launch, uninstall (uninstall stops the adb server first).
- [ ] Phone with an account → listed; remove it → *Check* → list empty → *Authenticate* enabled.
- [ ] Phone with a work profile or second user → listed as "User / profile".
- [ ] Phone without Killit → error with the tamodak.org link.
- [ ] Phone that already has a Device Owner → error, Authenticate stays disabled.
- [ ] Successful Authenticate → success message; Killit's home screen shows "Device Owner active".
- [ ] Xiaomi phone without "USB debugging (Security settings)" → the matching hint.
- [ ] Windows set to Arabic → mirrored layout; Turkish / German text fits the buttons.
