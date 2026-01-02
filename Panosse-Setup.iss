; Script Inno Setup pour Panosse
; Application de nettoyage automatique pour Windows
; Créé le 01/01/2025

#define MyAppName "Panosse"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Panosse"
#define MyAppURL "https://github.com/barbarom84-ai/panosse"
#define MyAppExeName "Panosse.exe"
#define MyAppIconFile "assets\panosse.ico"

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

[Files]
; Fichier principal (depuis le dossier publish)
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Ressources additionnelles (si nécessaire)
Source: "assets\panosse.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "assets\panosse.png"; DestDir: "{app}"; Flags: ignoreversion

; Fichiers de documentation (optionnels)
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme; DestName: "LisezMoi.txt"
Source: "PUBLICATION.md"; DestDir: "{app}"; Flags: ignoreversion; DestName: "Guide-Publication.txt"

[Icons]
; Icône dans le menu Démarrer
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\panosse.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; Icône sur le bureau
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\panosse.ico"; Tasks: desktopicon

; Icône dans la barre de lancement rapide
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\panosse.ico"; Tasks: quicklaunchicon

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
    // Code à exécuter après l'installation
  end;
end;

[CustomMessages]
french.LaunchProgram=Lancer %1 après l'installation
french.CreateDesktopIcon=Créer une icône sur le bureau
french.CreateQuickLaunchIcon=Créer une icône dans la barre de lancement rapide
french.AdditionalIcons=Icônes supplémentaires:

[Messages]
; Messages personnalisés en français
french.WelcomeLabel1=Bienvenue dans l'assistant d'installation de [name]
french.WelcomeLabel2=Ceci installera [name/ver] sur votre ordinateur.%n%nPanosse est une application de nettoyage automatique qui vous aide à garder votre PC propre et rapide.%n%nIl est recommandé de fermer toutes les autres applications avant de continuer.
french.FinishedHeadingLabel=Installation de [name] terminée
french.FinishedLabelNoIcons=L'installation de [name] est terminée.
french.FinishedLabel=L'installation de [name] est terminée. L'application peut être lancée en cliquant sur les icônes installées.
french.ClickFinish=Cliquez sur Terminer pour quitter l'assistant d'installation.

english.WelcomeLabel2=This will install [name/ver] on your computer.%n%nPanosse is an automatic cleaning application that helps keep your PC clean and fast.%n%nIt is recommended that you close all other applications before continuing.

