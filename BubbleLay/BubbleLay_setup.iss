[Setup]
AppName=BubbleLay
AppVersion=1.0
AppPublisher=Seyo
AppPublisherURL=https://github.com/Devseyo
AppSupportURL=https://github.com/Devseyo
DefaultDirName={pf}\BubbleLay
DefaultGroupName=BubbleLay
OutputDir=Output
OutputBaseFilename=BubbleLay_Installer
Compression=lzma
SolidCompression=yes
WizardStyle=modern
LicenseFile=LICENSE.txt

[Files]
Source: "C:\Users\Seyo\Downloads\Simple-Stream-Overlay-seyodev\Simple-Stream-Overlay-seyodev\BubbleLay\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\BubbleLay"; Filename: "{app}\BubbleLay.exe"
Name: "{commondesktop}\BubbleLay"; Filename: "{app}\BubbleLay.exe"

[Run]
Filename: "{app}\BubbleLay.exe"; Description: "Launch BubbleLay"; Flags: nowait postinstall skipifsilent