namespace Panosse.Services;

public static class RegistryIssueCategories
{
    public const string Privacy = "privacy";
    public const string Startup = "startup";
    public const string Uninstall = "uninstall";
    public const string AppPaths = "apppaths";
    public const string MuiCache = "muicache";
    public const string Compatibility = "compat";
    public const string SharedDlls = "shareddlls";

    public static string GetLabel(string category) => category switch
    {
        Privacy => "Historique d'activité",
        Startup => "Démarrage invalide",
        Uninstall => "Désinstallation orpheline",
        AppPaths => "Chemin d'application invalide",
        MuiCache => "Cache MUI obsolète",
        Compatibility => "Compatibilité obsolète",
        SharedDlls => "DLL partagée manquante",
        _ => category
    };
}
