using System.Windows;
using System.Windows.Media;

namespace Ludo.Presentation.Wpf.Views.Board;

/// <summary>
/// Draws the classic map-pin pawn: a glossy head tapering to a point that stands on the square, with a
/// soft shadow on the board that shrinks as the pawn jumps. Shapes are built once at unit size and scaled.
/// </summary>
internal static class PawnPainter
{
    private const double HeadY = -0.5;
    private const double HeadRadius = 0.28;

    private static readonly Geometry Body = BuildBody();
    private static readonly Brush ShadowBrush = Frozen(new SolidColorBrush(Color.FromArgb(0x55, 0, 0, 0)));
    private static readonly Brush GlossBrush = Frozen(new SolidColorBrush(Color.FromArgb(0x9C, 0xFF, 0xFF, 0xFF)));
    private static readonly Pen HighlightPen = Frozen(new Pen(Brushes.White, 0.07));

    /// <summary>Height of the pawn relative to its size, for hit testing and stacking.</summary>
    public const double Height = 0.8;

    /// <param name="foot">Where the pawn's point touches the board.</param>
    /// <param name="size">Pawn size in DIPs; it stands about 0.8 × size tall.</param>
    /// <param name="lift">How far above the board it is, in DIPs.</param>
    /// <param name="squash">0 at rest, 1 fully squashed on landing.</param>
    public static void Draw(DrawingContext dc, Point foot, double size, int arm, double lift, double squash)
    {
        var airborne = Math.Clamp(lift / size, 0, 1);
        var shadow = (1 - (airborne * 0.55)) * (1 + (squash * 0.15));
        dc.PushOpacity(1 - (airborne * 0.5));
        dc.DrawEllipse(ShadowBrush, null, foot, size * 0.24 * shadow, size * 0.085 * shadow);
        dc.Pop();

        var transform = Matrix.Identity;
        transform.Scale(size * (1 + (0.14 * squash)), size * (1 - (0.2 * squash)));
        transform.Translate(foot.X, foot.Y - lift);
        dc.PushTransform(new MatrixTransform(transform));

        var head = new Point(0, HeadY);
        dc.DrawGeometry(SeatPalette.PawnBrush(arm), SeatPalette.OutlinePen(arm), Body);
        dc.DrawEllipse(Brushes.White, null, head, 0.165, 0.165);
        dc.DrawEllipse(SeatPalette.Brush(arm), SeatPalette.OutlinePen(arm), head, 0.1, 0.1);
        dc.DrawEllipse(GlossBrush, null, new Point(-0.1, HeadY - 0.13), 0.075, 0.045);
        dc.Pop();
    }

    /// <summary>The pulsing ring under a pawn you can move.</summary>
    public static void DrawMoveRing(DrawingContext dc, Point foot, double size, double pulse)
    {
        var transform = Matrix.Identity;
        var grow = 1 + (pulse * 0.25);
        transform.Scale(size * grow, size * grow * 0.4);
        transform.Translate(foot.X, foot.Y);
        dc.PushTransform(new MatrixTransform(transform));
        dc.PushOpacity(0.95 - (pulse * 0.6));
        dc.DrawEllipse(null, HighlightPen, new Point(0, 0), 0.4, 0.4);
        dc.Pop();
        dc.Pop();
    }

    private static Geometry BuildBody()
    {
        // A circle for the head with two tangents down to the point at (0, 0).
        var distance = -HeadY;
        var angle = Math.Acos(HeadRadius / distance);
        var left = new Point(-HeadRadius * Math.Sin(angle), HeadY + (HeadRadius * Math.Cos(angle)));
        var right = new Point(HeadRadius * Math.Sin(angle), left.Y);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(0, 0), isFilled: true, isClosed: true);
            ctx.LineTo(left, isStroked: true, isSmoothJoin: true);
            ctx.ArcTo(right, new Size(HeadRadius, HeadRadius), 0, isLargeArc: true, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        return geometry;
    }

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
