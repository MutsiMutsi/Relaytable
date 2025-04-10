; Relaytable Application Installer Script
; Created with Inno Setup

#define MyAppName "Relaytable"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "Mutsi"
#define MyAppExeName "Relaytable.exe"
#define SourcePath "Relaytable\Relaytable\bin\Release\net8.0\win-x64\publish"

[Setup]
; Application information
AppId=193e131a-1b01-4d3c-a12e-a9a3b307b9d1
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://www.yourcompanywebsite.com/
AppSupportURL=https://www.yourcompanywebsite.com/support
AppUpdatesURL=https://www.yourcompanywebsite.com/updates
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Compress the output installer
Compression=lzma
SolidCompression=yes
; Create a desktop icon during installation
;CreateDesktopIcon=yes
; Require administrator privileges for installation
PrivilegesRequired=admin
; Output filename for the installer
OutputDir=Installer
OutputBaseFilename=Relaytable_Setup
; Setup icon (replace with path to your icon if available)
SetupIconFile=Relaytable\Relaytable\Assets\icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main executable
Source: "{#SourcePath}\Relaytable.exe"; DestDir: "{app}"; Flags: ignoreversion
; Required DLL files
Source: "{#SourcePath}\av_libglesv2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\libHarfBuzzSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\libSkiaSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: SetupIconFile
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: SetupIconFile

[Run]
; Option to run the application after installation is complete
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Dirs]
; Create AppData folder during installation
Name: "{localappdata}\Relaytable"; Flags: uninsalwaysuninstall

[InstallDelete]
; Clean up any previous installation files if necessary
Type: filesandordirs; Name: "{app}"

[UninstallDelete]
; Remove AppData folder during uninstallation
Type: filesandordirs; Name: "{localappdata}\Relaytable"