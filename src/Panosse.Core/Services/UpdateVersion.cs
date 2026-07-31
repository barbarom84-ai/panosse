namespace Panosse.Services;

/// <summary>
/// Normalizes GitHub release tags / FileVersion strings to major.minor.patch.
/// </summary>
public static class UpdateVersion
{
    public static string? Normalize(string? version)
    {
        if (string.IsNullOrWhiteSpace(version) ||
            version.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string trimmed = version.Trim().TrimStart('v', 'V');
        string[] parts = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}";
        }

        return trimmed.Length > 0 ? trimmed : null;
    }
}
