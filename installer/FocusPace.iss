#define MyAppName "Focus Pace"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "yuzhounh"
#define MyAppURL "https://github.com/yuzhounh/focus-pace"
#define MyAppExeName "FocusPace.exe"
#define MyAppRuntime "win-x64"
#ifndef MyAppSource
#define MyAppSource "..\artifacts\publish\win-x64\FocusPace.exe"
#endif

[Setup]
AppId={{FB0D354A-1155-49A9-9968-CA46CE33866C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\Focus Pace
DefaultGroupName=Focus Pace
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\artifacts\installer
OutputBaseFilename=FocusPace-{#MyAppVersion}-{#MyAppRuntime}-Setup
SetupIconFile=..\src\FocusPace\Assets\FocusPace.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Focus Pace installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCopyright=Copyright (c) 2026 {#MyAppPublisher}

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#MyAppSource}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Focus Pace"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Focus Pace"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Focus Pace"; Flags: nowait postinstall skipifsilent
