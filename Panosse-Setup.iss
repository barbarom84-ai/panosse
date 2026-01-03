; Script Inno Setup pour Panosse
; Application de nettoyage automatique pour Windows
; Créé le 01/01/2025

#define MyAppName "Panosse"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Panosse"
#define MyAppURL "https://github.com/barbarom84-ai/panosse"
#define MyAppExeName "Panosse.exe"
#define MyAppIconFile "assets\panosse.ico"
#define MyAppIconClean "assets\panosse_propre.ico"
#define MyAppIconDirty "assets\panosse_sale.ico"

[Setup]
; Informations de base de l'application
AppId={{8E5F4A3B-2D1C-4E9F-A7B6-3C8D9E2F1A4B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Dossier d'installation par défaut
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Icône de l'installateur et de l'application
SetupIconFile={#MyAppIconFile}
UninstallDisplayIcon={app}\{#MyAppExeName}

; Droits administrateur requis
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Fichier de sortie
OutputDir=installer
OutputBaseFilename=Panosse-Setup-v{#MyAppVersion}
SetupLogging=yes

; Compression
Compression=lzma2/max
SolidCompression=yes

; Interface moderne
WizardStyle=modern

; Langues
ShowLanguageDialog=auto

; Architecture
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Options d'installation
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
DisableWelcomePage=no
DisableReadyPage=no

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkablealone
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "{cm:AutoStartProgram,{#MyAppName}}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Fichier principal et toutes les DLLs natives (depuis le dossier publish)
Source: "bin\Release\net8.0-windows\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\win-x64\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; Fichiers icônes pour le System Tray (v2.0.0 - mémoire sélective)
; Copiés depuis le dossier assets source (pas depuis publish car embarqués)
Source: "{#MyAppIconClean}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppIconDirty}"; DestDir: "{app}"; Flags: ignoreversion

; Fichiers de documentation (optionnels)
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme; DestName: "LisezMoi.txt"

[Icons]
; Icône dans le menu Démarrer (avec icône propre)
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\panosse_propre.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; Icône sur le bureau (avec icône propre)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\panosse_propre.ico"; Tasks: desktopicon

; Icône dans la barre de lancement rapide
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\panosse_propre.ico"; Tasks: quicklaunchicon

[Registry]
; Lancer Panosse au démarrage de Windows (pour que Ctrl+Alt+P soit toujours actif)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Panosse"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
; Proposer de lancer l'application après installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser shellexec

[UninstallDelete]
; Nettoyer les fichiers temporaires créés par l'application (si nécessaire)
Type: filesandordirs; Name: "{app}"

[Code]
// Code Pascal personnalisé (optionnel)

// Vérifier si l'application est en cours d'exécution avant installation
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  // Vérifier si Panosse est en cours d'exécution
  if CheckForMutexes('PanosseAppMutex') then
  begin
    if MsgBox('Panosse est actuellement en cours d''exécution. Voulez-vous le fermer et continuer l''installation ?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      // Tenter de fermer l'application
      Exec('taskkill.exe', '/F /IM Panosse.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Result := True;
    end
    else
      Result := False;
  end;
end;

// Message de bienvenue personnalisé
procedure InitializeWizard();
begin
  // Vous pouvez ajouter du code personnalisé ici
end;

// Message après installation
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Afficher un message informatif après l'installation
    MsgBox('Panosse v2.0.0 a été installé avec succès !' + #13#10 + #13#10 + 
           'NOUVEAUTÉS v2.0.0 :' + #13#10 +
           '  - Raccourci global Ctrl+Alt+P pour nettoyer en arrière-plan' + #13#10 +
           '  - Icône System Tray avec changement d''état (propre/sale)' + #13#10 +
           '  - Surveillance intelligente du dossier Téléchargements' + #13#10 + #13#10 +
           'TIP : Si vous avez coché "Lancer au démarrage", le raccourci' + #13#10 +
           'Ctrl+Alt+P sera toujours disponible en arrière-plan !', 
           mbInformation, MB_OK);
  end;
end;

[CustomMessages]
french.LaunchProgram=Lancer %1 après l'installation
french.CreateDesktopIcon=Créer une icône sur le bureau
french.CreateQuickLaunchIcon=Créer une icône dans la barre de lancement rapide
french.AutoStartProgram=Lancer %1 au démarrage de Windows (recommandé pour Ctrl+Alt+P)
french.AdditionalIcons=Icônes supplémentaires:
english.AutoStartProgram=Start %1 with Windows (recommended for Ctrl+Alt+P hotkey)

[Messages]
; Messages personnalisés en français
french.WelcomeLabel1=Bienvenue dans l'assistant d'installation de [name]
french.WelcomeLabel2=Ceci installera [name/ver] sur votre ordinateur.%n%nPanosse est une application de nettoyage automatique qui vous aide à garder votre PC propre et rapide.%n%nNOUVEAUTÉS v2.0.0 :%n  - Raccourci global Ctrl+Alt+P%n  - Icône System Tray intelligente%n  - Surveillance automatique des Téléchargements%n%nIl est recommandé de fermer toutes les autres applications avant de continuer.
french.FinishedHeadingLabel=Installation de [name] terminée
french.FinishedLabelNoIcons=L'installation de [name] est terminée.
french.FinishedLabel=L'installation de [name] est terminée. L'application peut être lancée en cliquant sur les icônes installées.
french.ClickFinish=Cliquez sur Terminer pour quitter l'assistant d'installation.

english.WelcomeLabel2=This will install [name/ver] on your computer.%n%nPanosse is an automatic cleaning application that helps keep your PC clean and fast.%n%nNEW in v2.0.0:%n  - Global hotkey Ctrl+Alt+P%n  - Smart System Tray icon%n  - Automatic Downloads monitoring%n%nIt is recommended that you close all other applications before continuing.

