using System.Diagnostics;

namespace Ludo.Presentation.Wpf.Shell;

/// <summary>The project's home on GitHub: where the source lives and where releases are published.</summary>
internal static class ProjectLinks
{
    public const string Owner = "i-oms";
    public const string Repository = "dicedrop";

    public static Uri SourceCode { get; } = new($"https://github.com/{Owner}/{Repository}");

    /// <summary>Opens the page in the default browser, a separate process outside our capture protection.</summary>
    public static void OpenInBrowser(Uri page) =>
        Process.Start(new ProcessStartInfo(page.AbsoluteUri) { UseShellExecute = true })?.Dispose();
}
