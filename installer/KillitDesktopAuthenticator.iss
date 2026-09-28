; Inno Setup script for Killit Desktop Authenticator.
; Built by ../build.sh, which stages every input under StageDir and passes AppVersion.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef StageDir
  #define StageDir "..\build\stage"
#endif

#define AppName "Killit Desktop Authenticator"
#define AppExe "KillitDesktopAuthenticator.exe"

[Setup]
AppId={{6B0E3C1D-5F2A-4B8E-9C4D-2E7A1F3B9D60}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=tamodak
AppPublisherURL=https://tamodak.org
AppSupportURL=https://tamodak.org
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
; Admin rights: the USB driver and the runtime update both need them.
PrivilegesRequired=admin
; Windows 8 (6.2) and later.
MinVersion=6.2
OutputBaseFilename=KillitDesktopAuthenticator-Setup-{#AppVersion}
SetupIconFile={#StageDir}\app.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=auto
LanguageDetectionMethod=uilanguage
; Setup stays a 32-bit process (adb is 32-bit too), so {sys} resolves to the 32-bit system
; directory — the one adb loads the Universal C Runtime from. 64-bit tools go via {sysnative}.

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "pt"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ar"; MessagesFile: "compiler:Languages\Arabic.isl"

[CustomMessages]
en.TaskUsbDriver=Install the Google USB driver (needed on Windows 8 and for Google phones)
en.StatusDriver=Installing the Google USB driver...
en.StatusRuntime=Installing the Universal C Runtime update (KB2999226)...
en.DriverFailed=The Google USB driver could not be installed (code %1). If your phone is not found, install the USB driver from your phone's manufacturer.
en.RuntimeFailed=The Windows update KB2999226 (Universal C Runtime) could not be installed (code %1). adb needs it. Install all available updates with Windows Update, then run this setup again.

tr.TaskUsbDriver=Google USB sürücüsünü kur (Windows 8 ve Google telefonlar için gerekli)
tr.StatusDriver=Google USB sürücüsü kuruluyor...
tr.StatusRuntime=Universal C Runtime güncellemesi (KB2999226) kuruluyor...
tr.DriverFailed=Google USB sürücüsü kurulamadı (kod %1). Telefonun bulunamazsa, telefon üreticisinin USB sürücüsünü kur.
tr.RuntimeFailed=KB2999226 Windows güncellemesi (Universal C Runtime) kurulamadı (kod %1). adb bu güncellemeye ihtiyaç duyar. Windows Update ile tüm güncellemeleri kur, sonra bu kurulumu yeniden çalıştır.

de.TaskUsbDriver=Google-USB-Treiber installieren (nötig unter Windows 8 und für Google-Telefone)
de.StatusDriver=Google-USB-Treiber wird installiert...
de.StatusRuntime=Update für die Universal C Runtime (KB2999226) wird installiert...
de.DriverFailed=Der Google-USB-Treiber konnte nicht installiert werden (Code %1). Wird dein Telefon nicht gefunden, installiere den USB-Treiber des Herstellers.
de.RuntimeFailed=Das Windows-Update KB2999226 (Universal C Runtime) konnte nicht installiert werden (Code %1). adb benötigt es. Installiere alle verfügbaren Updates über Windows Update und starte dieses Setup erneut.

es.TaskUsbDriver=Instalar el controlador USB de Google (necesario en Windows 8 y para teléfonos Google)
es.StatusDriver=Instalando el controlador USB de Google...
es.StatusRuntime=Instalando la actualización de Universal C Runtime (KB2999226)...
es.DriverFailed=No se pudo instalar el controlador USB de Google (código %1). Si no se encuentra tu teléfono, instala el controlador USB del fabricante.
es.RuntimeFailed=No se pudo instalar la actualización de Windows KB2999226 (Universal C Runtime) (código %1). adb la necesita. Instala todas las actualizaciones con Windows Update y vuelve a ejecutar este instalador.

fr.TaskUsbDriver=Installer le pilote USB Google (nécessaire sous Windows 8 et pour les téléphones Google)
fr.StatusDriver=Installation du pilote USB Google...
fr.StatusRuntime=Installation de la mise à jour Universal C Runtime (KB2999226)...
fr.DriverFailed=Le pilote USB Google n'a pas pu être installé (code %1). Si ton téléphone n'est pas détecté, installe le pilote USB de son fabricant.
fr.RuntimeFailed=La mise à jour Windows KB2999226 (Universal C Runtime) n'a pas pu être installée (code %1). adb en a besoin. Installe toutes les mises à jour via Windows Update, puis relance cette installation.

pt.TaskUsbDriver=Instalar o controlador USB da Google (necessário no Windows 8 e para telemóveis Google)
pt.StatusDriver=A instalar o controlador USB da Google...
pt.StatusRuntime=A instalar a atualização Universal C Runtime (KB2999226)...
pt.DriverFailed=Não foi possível instalar o controlador USB da Google (código %1). Se o telemóvel não for encontrado, instala o controlador USB do fabricante.
pt.RuntimeFailed=Não foi possível instalar a atualização do Windows KB2999226 (Universal C Runtime) (código %1). O adb precisa dela. Instala todas as atualizações através do Windows Update e volta a executar esta instalação.

ru.TaskUsbDriver=Установить USB-драйвер Google (нужен в Windows 8 и для телефонов Google)
ru.StatusDriver=Установка USB-драйвера Google...
ru.StatusRuntime=Установка обновления Universal C Runtime (KB2999226)...
ru.DriverFailed=Не удалось установить USB-драйвер Google (код %1). Если телефон не обнаруживается, установите USB-драйвер его производителя.
ru.RuntimeFailed=Не удалось установить обновление Windows KB2999226 (Universal C Runtime) (код %1). Оно нужно для adb. Установите все обновления через Центр обновления Windows и запустите эту установку снова.

ar.TaskUsbDriver=تثبيت برنامج تشغيل USB من Google (مطلوب على Windows 8 ولهواتف Google)
ar.StatusDriver=جارٍ تثبيت برنامج تشغيل USB من Google...
ar.StatusRuntime=جارٍ تثبيت تحديث Universal C Runtime ‏(KB2999226)...
ar.DriverFailed=تعذّر تثبيت برنامج تشغيل USB من Google (الرمز %1). إذا لم يُعثر على هاتفك، فثبّت برنامج تشغيل USB من الشركة المصنّعة لهاتفك.
ar.RuntimeFailed=تعذّر تثبيت تحديث Windows ذي الرقم KB2999226 ‏(Universal C Runtime) (الرمز %1). يحتاج adb إليه. ثبّت كل التحديثات المتاحة عبر Windows Update، ثم شغّل برنامج الإعداد هذا مرة أخرى.

[Tasks]
Name: "usbdriver"; Description: "{cm:TaskUsbDriver}"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#StageDir}\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "{#StageDir}\adb\*"; DestDir: "{app}\adb"; Flags: ignoreversion
Source: "{#StageDir}\usb_driver\*"; DestDir: "{app}\usb_driver"; Flags: ignoreversion recursesubdirs; Tasks: usbdriver
; Extracted on demand by the [Code] below, only on Windows 8 / 8.1 without the runtime.
Source: "{#StageDir}\ucrt\*.msu"; Flags: dontcopy

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; The adb server keeps running after the program closes and would lock adb.exe.
Filename: "{app}\adb\adb.exe"; Parameters: "kill-server"; Flags: runhidden waituntilterminated; RunOnceId: "KillAdbServer"

[Code]
var
  RestartNeeded: Boolean;

procedure StopAdbServer(Dir: String);
var
  Code: Integer;
begin
  if FileExists(Dir + '\adb\adb.exe') then
    Exec(Dir + '\adb\adb.exe', 'kill-server', '', SW_HIDE, ewWaitUntilTerminated, Code);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  // An older copy's adb server would otherwise keep adb.exe locked during an upgrade.
  StopAdbServer(WizardDirValue);
  Result := '';
end;

{ Windows 8 (6.2) and 8.1 (6.3) lack the Universal C Runtime unless KB2999226 is installed. }
function RuntimeMsuName: String;
var
  Version: TWindowsVersion;
  Arch: String;
begin
  Result := '';
  GetWindowsVersionEx(Version);
  if (Version.Major <> 6) or FileExists(ExpandConstant('{sys}\ucrtbase.dll')) then
    exit;
  if IsWin64 then Arch := 'x64' else Arch := 'x86';
  if Version.Minor = 2 then
    Result := 'Windows8-RT-KB2999226-' + Arch + '.msu'
  else if Version.Minor = 3 then
    Result := 'Windows8.1-KB2999226-' + Arch + '.msu';
end;

procedure InstallRuntime;
var
  Msu: String;
  Code: Integer;
begin
  Msu := RuntimeMsuName;
  if Msu = '' then
    exit;

  WizardForm.StatusLabel.Caption := CustomMessage('StatusRuntime');
  ExtractTemporaryFile(Msu);
  if not Exec(ExpandConstant('{sysnative}\wusa.exe'), '"' + ExpandConstant('{tmp}\' + Msu) + '" /quiet /norestart',
              ExpandConstant('{tmp}'), SW_HIDE, ewWaitUntilTerminated, Code) then
    Code := -1;

  // 3010 = installed, restart required; 2359302 (0x00240006) = already installed.
  if Code = 3010 then
    RestartNeeded := True
  else if (Code <> 0) and (Code <> 2359302) then
    MsgBox(FmtMessage(CustomMessage('RuntimeFailed'), [IntToStr(Code)]), mbError, MB_OK);
end;

procedure InstallUsbDriver;
var
  Code: Integer;
begin
  WizardForm.StatusLabel.Caption := CustomMessage('StatusDriver');
  // "-i -a" is the syntax Windows 8's pnputil knows; Windows 10 and 11 still accept it.
  if not Exec(ExpandConstant('{sysnative}\pnputil.exe'),
              '-i -a "' + ExpandConstant('{app}\usb_driver\android_winusb.inf') + '"',
              ExpandConstant('{tmp}'), SW_HIDE, ewWaitUntilTerminated, Code) then
    Code := -1;

  // 259 = added to the driver store but no matching phone is plugged in yet; 3010 = restart.
  if Code = 3010 then
    RestartNeeded := True
  else if (Code <> 0) and (Code <> 259) then
    MsgBox(FmtMessage(CustomMessage('DriverFailed'), [IntToStr(Code)]), mbInformation, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    InstallRuntime;
    if WizardIsTaskSelected('usbdriver') then
      InstallUsbDriver;
  end;
end;

function NeedRestart: Boolean;
begin
  Result := RestartNeeded;
end;
