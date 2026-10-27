using System.Windows;
using System.Windows.Media;

namespace Ludo.Presentation.Wpf.Views.Board;

/// <summary>
/// Colours by board arm, in the traditional clockwise order red, green, yellow, blue, then four more for
/// bigger boards. Each colour comes with lighter and darker shades for the pawns' shading.
/// </summary>
internal static class SeatPalette
{
    private static readonly Color[] Colors =
    [
        Color.FromRgb(0xE5, 0x32, 0x2D), // red
        Color.FromRgb(0x2E, 0xA8, 0x4A), // green
        Color.FromRgb(0xF7, 0xC3, 0x1C), // yellow
        Color.FromRgb(0x1F, 0x7A, 0xE0), // blue
        Color.FromRgb(0x8E, 0x3C, 0xC8), // purple
        Color.FromRgb(0xF2, 0x7A, 0x12), // orange
        Color.FromRgb(0x10, 0xA8, 0xB4), // teal
        Color.FromRgb(0xE0, 0x3A, 0x8A), // pink
    ];

    private static readonly Brush[] Solid = Colors.Select(c => Freeze(new SolidColorBrush(c))).ToArray();
    private static readonly Brush[] Muted = Colors.Select(c => Freeze(new SolidColorBrush(Mix(c, Color.FromRgb(0xC9, 0xC6, 0xBF), 0.62)))).ToArray();
    private static readonly Brush[] LightBrushes = Colors.Select(c => Freeze(new SolidColorBrush(Mix(c, System.Windows.Media.Colors.White, 0.55)))).ToArray();
    private static readonly Brush[] PawnBrushes = Colors.Select(PawnFill).ToArray();
    private static readonly Pen[] OutlinePens = Colors.Select(c => Freeze(new Pen(new SolidColorBrush(Mix(c, System.Windows.Media.Colors.Black, 0.45)), 0.045))).ToArray();

    public static Color ColorOf(int arm) => Colors[arm % Colors.Length];

    public static Brush Brush(int arm) => Solid[arm % Solid.Length];

    /// <summary>The arm's colour faded towards grey, for arms nobody plays from.</summary>
    public static Brush MutedBrush(int arm) => Muted[arm % Muted.Length];

    public static Brush LightBrush(int arm) => LightBrushes[arm % LightBrushes.Length];

    /// <summary>Glossy fill for a pawn: lit from the top left.</summary>
    public static Brush PawnBrush(int arm) => PawnBrushes[arm % PawnBrushes.Length];

    /// <summary>Outline in a dark shade of the colour, sized for a pawn drawn at unit size and scaled up.</summary>
    public static Pen OutlinePen(int arm) => OutlinePens[arm % OutlinePens.Length];

    public static Color Mix(Color a, Color b, double amount) => Color.FromArgb(
        (byte)(a.A + ((b.A - a.A) * amount)),
        (byte)(a.R + ((b.R - a.R) * amount)),
        (byte)(a.G + ((b.G - a.G) * amount)),
        (byte)(a.B + ((b.B - a.B) * amount)));

    private static Brush PawnFill(Color c)
    {
        var brush = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.32, 0.22),
            Center = new Point(0.42, 0.35),
            RadiusX = 0.75,
            RadiusY = 0.75,
            GradientStops =
            [
                new GradientStop(Mix(c, System.Windows.Media.Colors.White, 0.55), 0),
                new GradientStop(c, 0.45),
                new GradientStop(Mix(c, System.Windows.Media.Colors.Black, 0.35), 1),
            ],
        };
        return Freeze(brush);
    }

    private static T Freeze<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
