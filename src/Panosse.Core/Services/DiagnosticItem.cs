namespace Panosse.Services;

public sealed class DiagnosticItem
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "ok";
    public string Detail { get; set; } = string.Empty;

    public string DisplayLine =>
        Status switch
        {
            "ok" => $"✅ {Name} — {Detail}",
            "warning" => $"⚠️ {Name} — {Detail}",
            _ => $"❌ {Name} — {Detail}"
        };
}
