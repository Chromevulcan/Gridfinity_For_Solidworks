; Inno Setup script for the Gridfinity for SolidWorks add-in.
; Needs Inno Setup 6.3 or newer (free, https://jrsoftware.org/isinfo.php). Build the solution in Release first,
; then open this file in Inno Setup and press Compile (or run scripts\make-installer.cmd).
; Output: installer\Output\GridfinityForSolidWorks-Setup.exe
;
; What the installer does:
;   1. Looks for your SolidWorks installation and asks you to confirm (or browse to) the folder.
;   2. Suggests a "Gridfinity for SolidWorks" folder inside it as the install location, which you confirm.
;   3. Copies the add-in, then copies the three SolidWorks interop DLLs it needs from YOUR SolidWorks install
;      (they are not bundled), and registers the add-in with SolidWorks.

#define AppName "Gridfinity for SolidWorks"
#define AppVersion "1.0.0"
#define BuildDir "..\src\Gridfinity.Sw\bin\Release\net48"

[Setup]
AppId={{B7E0D7C1-52A4-4D49-8C5E-3A1F5E0C9D21}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Gridfinity for SolidWorks
DefaultDirName={autopf}\Gridfinity for SolidWorks
DisableDirPage=no
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=GridfinityForSolidWorks-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; Registering a COM add-in writes to HKLM, so the installer needs administrator rights.
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#AppName}

[Messages]
WelcomeLabel2=This installs the Gridfinity tray, baseplate and clip builder as a SolidWorks add-in.%n%nPlease close SolidWorks before continuing. After installing, start SolidWorks and look for Tools > Gridfinity (if it is missing, tick Gridfinity Tray Builder under Tools > Add-Ins).

[Files]
; The add-in and the libraries it carries. The SolidWorks interop DLLs are copied from your SolidWorks install instead.
Source: "{#BuildDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml,SolidWorks.Interop.*.dll"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\samples\*"; DestDir: "{app}\samples"; Flags: ignoreversion
Source: "..\trays\*"; DestDir: "{app}\trays"; Flags: ignoreversion
Source: "..\data\*"; DestDir: "{app}\data"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[UninstallRun]
Filename: "{dotnet4064}\regasm.exe"; Parameters: "/unregister ""{app}\Gridfinity.Sw.dll"""; Flags: runhidden waituntilterminated; RunOnceId: "UnregisterGridfinity"

[Icons]
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

[Code]
var
  SwPage: TInputDirWizardPage;
  DirSuggested: Boolean;

const
  InteropA = 'SolidWorks.Interop.sldworks.dll';
  InteropB = 'SolidWorks.Interop.swconst.dll';
  InteropC = 'SolidWorks.Interop.swpublished.dll';

// The folder that holds SLDWORKS.exe, or '' when none is found. SolidWorks records it under
// HKLM\SOFTWARE\SolidWorks\SOLIDWORKS <year>\Setup; the newest version wins.
function FindSolidWorks(): String;
var
  Keys: TArrayOfString;
  I: Integer;
  Loc: String;
begin
  Result := '';
  if RegGetSubkeyNames(HKLM, 'SOFTWARE\SolidWorks', Keys) then
    for I := 0 to GetArrayLength(Keys) - 1 do
      if Pos('SOLIDWORKS', Uppercase(Keys[I])) = 1 then
        if RegQueryStringValue(HKLM, 'SOFTWARE\SolidWorks\' + Keys[I] + '\Setup', 'SolidWorks Location', Loc) then
        begin
          Loc := RemoveBackslash(Loc);
          if FileExists(Loc + '\SLDWORKS.exe') then Result := Loc;
        end;

  if Result = '' then
    if FileExists(ExpandConstant('{commonpf64}') + '\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe') then
      Result := ExpandConstant('{commonpf64}') + '\SOLIDWORKS Corp\SOLIDWORKS';
end;

function SolidWorksOk(Dir: String): Boolean;
begin
  Dir := RemoveBackslash(Dir);
  Result := FileExists(Dir + '\SLDWORKS.exe') and FileExists(Dir + '\api\redist\' + InteropA)
            and FileExists(Dir + '\api\redist\' + InteropB) and FileExists(Dir + '\api\redist\' + InteropC);
end;

procedure InitializeWizard();
var
  Found: String;
begin
  SwPage := CreateInputDirPage(wpWelcome, 'SolidWorks installation',
    'Where is SolidWorks installed?',
    'Setup found the folder below. Check that it is the folder containing SLDWORKS.exe for the SolidWorks you use, ' +
    'or click Browse and choose it, then click Next to confirm.', False, '');
  SwPage.Add('SolidWorks folder:');

  Found := FindSolidWorks();
  if Found = '' then
    SwPage.Values[0] := ExpandConstant('{commonpf64}') + '\SOLIDWORKS Corp\SOLIDWORKS'
  else
    SwPage.Values[0] := Found;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = SwPage.ID then
    if not SolidWorksOk(SwPage.Values[0]) then
    begin
      MsgBox('SLDWORKS.exe and the api\redist interop files were not found in:' + #13#10 + SwPage.Values[0] + #13#10#13#10 +
             'Choose the SolidWorks folder that contains SLDWORKS.exe (it has an "api\redist" folder inside).', mbError, MB_OK);
      Result := False;
    end;
end;

// Suggest an install folder inside the confirmed SolidWorks folder; the user still confirms it on the next page.
procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpSelectDir) and not DirSuggested then
  begin
    DirSuggested := True;
    WizardForm.DirEdit.Text := RemoveBackslash(SwPage.Values[0]) + '\Gridfinity for SolidWorks';
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Redist, AppDir: String;
  Code: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    Redist := RemoveBackslash(SwPage.Values[0]) + '\api\redist\';
    AppDir := ExpandConstant('{app}') + '\';
    FileCopy(Redist + InteropA, AppDir + InteropA, False);
    FileCopy(Redist + InteropB, AppDir + InteropB, False);
    FileCopy(Redist + InteropC, AppDir + InteropC, False);

    WizardForm.StatusLabel.Caption := 'Registering the add-in with SolidWorks...';
    if (not Exec(ExpandConstant('{dotnet4064}') + '\regasm.exe', '/codebase "' + AppDir + 'Gridfinity.Sw.dll"', '',
                 SW_HIDE, ewWaitUntilTerminated, Code)) or (Code <> 0) then
      MsgBox('The files were installed, but registering the add-in with SolidWorks failed (code ' + IntToStr(Code) + ').' + #13#10 +
             'Run this installer again as administrator, or run scripts\register.cmd.', mbError, MB_OK);
  end;
end;
