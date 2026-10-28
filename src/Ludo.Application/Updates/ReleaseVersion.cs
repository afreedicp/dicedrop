using System.Globalization;
using System.Text.RegularExpressions;

namespace Ludo.Application.Updates;

/// <summary>
/// A release version in the only tag format the release workflow accepts: vMAJOR.MINOR.PATCH, a lowercase "v"
/// and three numbers without leading zeros.
/// </summary>
public static partial class ReleaseVersion
{
    public static bool TryParseTag(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);
        var match = tag is null ? null : TagFormat().Match(tag);
        if (match is not { Success: true })
        {
            return false;
        }

        version = new Version(
            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
        return true;
    }

    /// <summary>Major.minor.patch only: assembly versions carry a fourth part that releases never set.</summary>
    public static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0));

    // Same pattern as .github/workflows/release.yml; eight digits per part keeps every value inside an int.
    [GeneratedRegex(@"^v(0|[1-9]\d{0,7})\.(0|[1-9]\d{0,7})\.(0|[1-9]\d{0,7})\z", RegexOptions.CultureInvariant)]
    private static partial Regex TagFormat();
}
