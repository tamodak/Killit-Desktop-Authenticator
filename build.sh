#!/usr/bin/env bash
# Builds dist/KillitDesktopAuthenticator-Setup-<version>.exe on Linux.
#
# Needs: dotnet SDK 8, wine (with wine32), curl, unzip, sha256sum.
# Downloads are pinned by SHA-256 and cached in build/deps; Inno Setup is installed into a private
# Wine prefix under build/wine, so nothing outside this directory is touched.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

PROJECT=src/KillitDesktopAuthenticator/KillitDesktopAuthenticator.csproj
TESTS=tests/KillitDesktopAuthenticator.Tests
VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PROJECT")"

DEPS="$ROOT/build/deps"
STAGE="$ROOT/build/stage"
DIST="$ROOT/dist"
export WINEPREFIX="$ROOT/build/wine"
export WINEDEBUG=-all
# Don't let a fresh prefix prompt to install Wine Mono / Gecko; ISCC needs neither.
export WINEDLLOVERRIDES="mscoree,mshtml="
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# platform-tools r34.0.5 is the newest adb that still runs on Windows 8 / 8.1: later versions import
# ProcessPrng from bcryptprimitives.dll, which only exists on Windows 10 and later.
DOWNLOADS=(
  "platform-tools_r34.0.5-windows.zip|https://dl.google.com/android/repository/platform-tools_r34.0.5-windows.zip|3f8320152704377de150418a3c4c9d07d16d80a6c0d0d8f7289c22c499e33571"
  "usb_driver_r13-windows.zip|https://dl.google.com/android/repository/usb_driver_r13-windows.zip|360b01d3dfb6c41621a3a64ae570dfac2c9a40cca1b5a1f136ae90d02f5e9e0b"
  "innosetup-6.7.3.exe|https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe|9c73c3bae7ed48d44112a0f48e66742c00090bdb5bef71d9d3c056c66e97b732"
  "Windows8-RT-KB2999226-x86.msu|https://download.microsoft.com/download/1/e/8/1e8afe90-5217-464d-9292-7d0b95a56ce4/Windows8-RT-KB2999226-x86.msu|0f36750fbb06fee23131f68b4d0943841eed24730ec1d5d77dedc41d359be88d"
  "Windows8-RT-KB2999226-x64.msu|https://download.microsoft.com/download/a/c/1/ac15393f-a6e6-469b-b222-c44b3bb6eccc/Windows8-RT-KB2999226-x64.msu|50cae25da33fa950222d1a803e42567291eb7feb087fa119b1c97fe9d41cd9f8"
  "Windows8.1-KB2999226-x86.msu|https://download.microsoft.com/download/e/4/6/e4694323-8290-4a08-82db-81f2eb9452c2/Windows8.1-KB2999226-x86.msu|b83251219c5390536b02bebaf5e43a6f13381ce1db43e76483bce07c4bcf877b"
  "Windows8.1-KB2999226-x64.msu|https://download.microsoft.com/download/9/6/f/96fd0525-3ddf-423d-8845-5f92f4a6883e/Windows8.1-KB2999226-x64.msu|9f707096c7d279ed4bc2a40ba695efac69c20406e0ca97e2b3e08443c6381d15"
)

step() { printf '\n==> %s\n' "$*"; }

fetch() {
  local name="$1" url="$2" sha="$3" file="$DEPS/$1"
  if [[ -f "$file" ]] && echo "$sha  $file" | sha256sum -c --status; then
    return
  fi
  echo "downloading $name"
  curl -fL --retry 3 -o "$file.part" "$url"
  if ! echo "$sha  $file.part" | sha256sum -c --status; then
    echo "SHA-256 mismatch for $name" >&2
    rm -f "$file.part"
    exit 1
  fi
  mv "$file.part" "$file"
}

step "Downloading pinned dependencies"
mkdir -p "$DEPS"
for entry in "${DOWNLOADS[@]}"; do
  IFS='|' read -r name url sha <<<"$entry"
  fetch "$name" "$url" "$sha"
done

step "Running unit tests"
dotnet test "$TESTS" -c Release

step "Building the program ($VERSION)"
rm -rf "$STAGE"
mkdir -p "$STAGE"/{app,adb,usb_driver,ucrt}
dotnet build "$PROJECT" -c Release -o "$STAGE/build"
cp "$STAGE/build/KillitDesktopAuthenticator.exe" "$STAGE/build/KillitDesktopAuthenticator.exe.config" "$STAGE/app/"
for lang in "$STAGE"/build/*/; do
  lang="$(basename "$lang")"
  mkdir -p "$STAGE/app/$lang"
  cp "$STAGE/build/$lang/"*.resources.dll "$STAGE/app/$lang/"
done
rm -rf "$STAGE/build"
cp src/KillitDesktopAuthenticator/app.ico "$STAGE/app.ico"

step "Staging adb, USB driver and runtime update"
unzip -q -j -o "$DEPS/platform-tools_r34.0.5-windows.zip" \
  platform-tools/adb.exe platform-tools/AdbWinApi.dll platform-tools/AdbWinUsbApi.dll platform-tools/NOTICE.txt \
  -d "$STAGE/adb"
unzip -q -o "$DEPS/usb_driver_r13-windows.zip" -d "$STAGE"
cp "$DEPS"/*.msu "$STAGE/ucrt/"

step "Preparing Inno Setup under Wine"
ISCC="$WINEPREFIX/drive_c/innosetup/ISCC.exe"
if [[ ! -f "$ISCC" ]]; then
  wineboot -i >/dev/null 2>&1 || true
  wine "$DEPS/innosetup-6.7.3.exe" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CURRENTUSER \
    '/DIR=C:\innosetup' >/dev/null 2>&1
  wineserver -w
fi
[[ -f "$ISCC" ]] || { echo "Inno Setup did not install into $WINEPREFIX" >&2; exit 1; }

step "Compiling the installer"
mkdir -p "$DIST"
wine "$ISCC" /Q \
  "/DAppVersion=$VERSION" \
  "/DStageDir=$(winepath -w "$STAGE")" \
  "/O$(winepath -w "$DIST")" \
  "$(winepath -w "$ROOT/installer/KillitDesktopAuthenticator.iss")"
wineserver -w

step "Done"
ls -l "$DIST/KillitDesktopAuthenticator-Setup-$VERSION.exe"
