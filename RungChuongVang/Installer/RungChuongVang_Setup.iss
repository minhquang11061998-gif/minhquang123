; Kịch bản Inno Setup tạo bộ cài đặt .exe chuẩn cho Windows 10/11 (64-bit)
; Yêu cầu: Đã chạy build-and-publish.bat để có thư mục ..\publish\RungChuongVang_Win64

#define MyAppName "Phần mềm Rung Chuông Vàng"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Ban Tổ Chức Rung Chuông Vàng"
#define MyAppExeName "RungChuongVang.exe"

[Setup]
AppId={{8F9E6A12-3B7D-4C22-910E-20261113RCV1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\RungChuongVang
DefaultGroupName={#MyAppName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\publish\Installer
OutputBaseFilename=Setup_RungChuongVang_x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Tạo biểu tượng ngoài màn hình Desktop"; GroupDescription: "Tùy chọn bổ sung:"

[Files]
Source: "..\publish\RungChuongVang_Win64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
; Cấp quyền ghi cho thư mục Data khi cài đặt trong Program Files hoặc chạy tại thư mục cài đặt
Name: "{app}\Data"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Hướng dẫn sử dụng"; Filename: "{app}\Docs\HUONG_DAN_SU_DUNG.md"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Khởi chạy {#MyAppName}"; Flags: nowait postinstall skipifsilent
