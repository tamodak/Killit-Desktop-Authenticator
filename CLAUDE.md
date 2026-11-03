# Killit Desktop Authenticator

## What this is

A Windows program (Windows 8, 8.1, 10, 11) that makes Killit the **Device Owner** of an Android phone
over USB: the `adb shell dpm set-device-owner` step, without a terminal, for people who can't or won't
use Shizuku, QR provisioning or a command line. It checks the phone for everything Android would
refuse (accounts, extra users, an existing Device Owner) before it runs the command.

It works with `../Killit/`: the package `org.tamodak.killit` and its admin component
`org.tamodak.killit/.admin.KillitDeviceAdminReceiver` are what it provisions
(`../Killit/docs/device-owner.md`).

## Current state (2026-11-03)

- Version 1.0.0; the installer was built on 2026-09-28. Not published yet.
- Its checklist of tests on real Windows machines (in [README.md](README.md)) hasn't been run.

## Build, run, test

On Linux:

```bash
sudo apt install dotnet-sdk-8.0 wine wine64 wine32:i386   # once; needs i386 enabled
./build.sh                                   # → dist/KillitDesktopAuthenticator-Setup-<version>.exe
dotnet test tests/KillitDesktopAuthenticator.Tests
```

`build.sh` downloads and verifies every dependency, runs the tests, builds the program, and compiles
the installer with Inno Setup in a private Wine prefix. Nothing outside the project folder is touched.

## How it works

- `Adb/`: runs the bundled `adb.exe`, parses what adb, `dumpsys`, `pm` and `dpm` print (unit tested),
  and holds the Check and Authenticate workflow (`Provisioner.cs`), which tells adb's own errors
  (unplugged, unauthorized, offline) apart from a command's output.
- `MainForm.cs`: the window. `Properties/Strings*.resx`: the text, one file per language.
- `installer/`: the Inno Setup script.

How it works, step by step, and why each component has its version: [README.md](README.md).

## Conventions

- **Keep matching Killit:** the package and the admin component are constants in
  `Adb/Provisioner.cs`. If Killit's admin component ever moves, change both repositories.
- **Every bundled download is pinned by SHA-256** in `build.sh`. Update the hash with the version,
  after verifying the download.
- **Every UI string goes into all nine `.resx` files**, the same nine languages as Killit
  (`../Killit/docs/languages.md`). Arabic is right to left.
- **Compatibility first:** the program targets .NET Framework 4.5 and bundles adb r34.0.5, the newest
  that still runs on Windows 8. Don't raise either without a reason that outweighs Windows 8.
- adb's `NOTICE.txt` ships with the installer.

## Docs

- [README.md](README.md): how it works, the compatibility choices, building, the layout, and the
  Windows checklist.
- [docs/README.md](docs/README.md): the index; no other docs are needed yet.
