using System.Windows;
using System.Windows.Media;
using Ludo.Application.Session;

namespace Ludo.Presentation.Wpf.Views.Board;

/// <summary>A board square: a centre, two unit axes and a half-size, so it can sit at any angle.</summary>
internal readonly record struct Quad(Point Center, Vector U, Vector V, double Half)
{
    public Point[] Corners(double inset = 0)
    {
        var h = Half - inset;
        return
        [
            Center - (U * h) - (V * h),
            Center + (U * h) - (V * h),
            Center + (U * h) + (V * h),
            Center - (U * h) + (V * h),
        ];
    }
}

/// <summary>
/// Lays out the classic cross-shaped Ludo board, generalised to any number of arms. Each arm is a strip
/// three squares wide and six long, standing on one side of a regular polygon in the middle: with four arms
/// that is exactly the traditional 15 × 15 board, with six or eight it becomes a hexagonal or octagonal star.
/// Each yard fills the wedge between its arm and the next one clockwise, next to its start square, and the
/// board is turned so the viewer's own yard sits bottom left.
/// </summary>
internal sealed class BoardGeometry
{
    private const int Lanes = 3;
    private const int ArmLength = 6;
    private const int SquaresPerArm = (2 * ArmLength) + 1;

    /// <summary>The start square is the second square in from the tip on the inbound lane.</summary>
    private const int StartInArm = ArmLength + 2;

    private readonly BoardLayoutView _layout;
    private readonly int _arms;
    private readonly double _apothem;
    private readonly double _turn;

    public BoardGeometry(BoardLayoutView layout, Size size, int viewerArm, double pixelsPerDip)
    {
        _layout = layout;
        _arms = layout.ArmCount;
        _apothem = Lanes / 2.0 / Math.Tan(Math.PI / _arms);

        // Arm j points along angle turn + j·2π/n (screen angles run clockwise). The viewer's yard is the
        // wedge after its arm, centred half a step further on; put that wedge at 135°, i.e. bottom left.
        _turn = (0.75 * Math.PI) - (Math.PI / _arms) - (viewerArm * 2 * Math.PI / _arms);

        IsClassic = _arms == 4;
        var extent = IsClassic ? 7.5 : MaxExtent();

        // Work in whole device pixels so the classic grid lands exactly on the pixel grid.
        var widthPx = Math.Floor(size.Width * pixelsPerDip);
        var heightPx = Math.Floor(size.Height * pixelsPerDip);
        var cellPx = Math.Floor(Math.Min(widthPx, heightPx) / (2 * extent));
        if (!IsClassic)
        {
            cellPx = Math.Min(widthPx, heightPx) / (2 * extent);
        }

        Cell = cellPx / pixelsPerDip;
        var boardPx = cellPx * 2 * extent;
        var originX = Math.Round((widthPx - boardPx) / 2);
        var originY = Math.Round((heightPx - boardPx) / 2);
        Center = new Point((originX + (boardPx / 2)) / pixelsPerDip, (originY + (boardPx / 2)) / pixelsPerDip);
        Bounds = new Rect(originX / pixelsPerDip, originY / pixelsPerDip, boardPx / pixelsPerDip, boardPx / pixelsPerDip);
        PixelsPerDip = pixelsPerDip;
    }

    public bool IsClassic { get; }

    public double PixelsPerDip { get; }

    public int ArmCount => _arms;

    public Point Center { get; }

    public double Cell { get; }

    /// <summary>Square the board occupies; the board itself fills it for four arms.</summary>
    public Rect Bounds { get; }

    /// <summary>Outward direction of an arm.</summary>
    public Vector Out(int arm)
    {
        var angle = _turn + (arm * 2 * Math.PI / _arms);
        return Snap(new Vector(Math.Cos(angle), Math.Sin(angle)));
    }

    /// <summary>Across an arm, pointing at its inbound lane (and at the yard after it).</summary>
    public Vector Across(int arm)
    {
        var o = Out(arm);
        return new Vector(-o.Y, o.X);
    }

    /// <summary>Square of an arm by lane (-1 outbound, 0 middle, +1 inbound) and ring (0 next to the centre, 5 at the tip).</summary>
    public Quad ArmSquare(int arm, int lane, int ring) =>
        new(Center + (Out(arm) * (_apothem + ring + 0.5) * Cell) + (Across(arm) * lane * Cell), Out(arm), Across(arm), Cell / 2);

    public Quad TrackSquare(int square)
    {
        var shifted = (square + StartInArm) % (SquaresPerArm * _arms);
        var arm = shifted / SquaresPerArm;
        var step = shifted % SquaresPerArm;
        return step switch
        {
            < ArmLength => ArmSquare(arm, -1, step),
            ArmLength => ArmSquare(arm, 0, ArmLength - 1),
            _ => ArmSquare(arm, 1, (2 * ArmLength) - step),
        };
    }

    public Quad HomeSquare(int arm, int step) => ArmSquare(arm, 0, _layout.HomeColumnLength - 1 - step);

    /// <summary>The arm's tip square, where its owner turns into the home column.</summary>
    public Quad TurnSquare(int arm) => ArmSquare(arm, 0, ArmLength - 1);

    public int StartSquare(int arm) => arm * _layout.SquaresPerArm;

    public bool IsStarSquare(int square) => square % _layout.SquaresPerArm == _layout.StarOffset;

    public bool IsStartSquare(int square) => square % _layout.SquaresPerArm == 0;

    public int ArmOfSquare(int square) => square / _layout.SquaresPerArm;

    /// <summary>The middle polygon's two corners on an arm's side, outbound side first.</summary>
    public (Point Outbound, Point Inbound) InnerEdge(int arm)
    {
        var foot = Center + (Out(arm) * _apothem * Cell);
        var half = Across(arm) * (Lanes / 2.0) * Cell;
        return (foot - half, foot + half);
    }

    /// <summary>The yard after an arm: a rhombus (a square on the classic board) between it and the next arm.</summary>
    public Point[] Yard(int arm, double scale = 1)
    {
        var corner = InnerEdge(arm).Inbound;
        var a = Out(arm) * ArmLength * Cell;
        var b = Out((arm + 1) % _arms) * ArmLength * Cell;
        Point[] points = [corner, corner + a, corner + a + b, corner + b];
        if (scale == 1)
        {
            return points;
        }

        var middle = YardCenter(arm);
        return points.Select(p => middle + ((p - middle) * scale)).ToArray();
    }

    public Point YardCenter(int arm)
    {
        var corner = InnerEdge(arm).Inbound;
        return corner + ((Out(arm) + Out((arm + 1) % _arms)) * (ArmLength / 2.0) * Cell);
    }

    /// <summary>Where a token waits in its yard: the four spots of the white inner area.</summary>
    public Point YardSlot(int arm, int index)
    {
        var a = Out(arm);
        var b = Out((arm + 1) % _arms);
        var count = Math.Max(_layout.TokensPerPlayer, 1);
        if (count == 4)
        {
            var (s, t) = index switch { 0 => (-1, -1), 1 => (-1, 1), 2 => (1, -1), _ => (1, 1) };
            return YardCenter(arm) + (((a * s) + (b * t)) * Cell);
        }

        var angle = (2 * Math.PI * index / count) + (Math.PI / 4);
        return YardCenter(arm) + (((a * Math.Cos(angle)) + (b * Math.Sin(angle))) * 1.25 * Cell);
    }

    /// <summary>The triangle of the middle polygon that an arm's home column runs into.</summary>
    public Point[] HomeTriangle(int arm)
    {
        var (outbound, inbound) = InnerEdge(arm);
        return [Center, outbound, inbound];
    }

    /// <summary>Spot for a finished token, spread across its home triangle.</summary>
    public Point FinishSpot(int arm, int index)
    {
        var (outbound, inbound) = InnerEdge(arm);
        var count = Math.Max(_layout.TokensPerPlayer, 1);
        var t = count == 1 ? 0.5 : (index + 0.5) / count;
        var edge = outbound + ((inbound - outbound) * t);
        return Center + ((edge - Center) * 0.62);
    }

    /// <summary>Where a token stands for a given progress along its owner's path.</summary>
    public Point PathPoint(SeatView seat, int tokenIndex, int progress)
    {
        var lastTrack = _layout.MainTrackLength - 2;
        if (progress < 0)
        {
            return YardSlot(seat.Arm, tokenIndex);
        }

        if (progress <= lastTrack)
        {
            return TrackSquare((StartSquare(seat.Arm) + progress) % _layout.MainTrackLength).Center;
        }

        var step = progress - lastTrack - 1;
        return step < _layout.HomeColumnLength
            ? HomeSquare(seat.Arm, step).Center
            : FinishSpot(seat.Arm, tokenIndex);
    }

    private double MaxExtent()
    {
        // Lay the board out at unit cell size around (0,0) and measure how far it reaches.
        var reach = 0.0;
        for (var arm = 0; arm < _arms; arm++)
        {
            var o = Out(arm);
            var next = Out((arm + 1) % _arms);
            var corner = (o * _apothem) + (Across(arm) * (Lanes / 2.0));
            Vector[] points =
            [
                corner + (o * ArmLength),
                corner + (o * ArmLength) + (next * ArmLength),
                corner + (next * ArmLength),
                (o * (_apothem + ArmLength)) - (Across(arm) * (Lanes / 2.0)),
            ];
            foreach (var p in points)
            {
                reach = Math.Max(reach, Math.Max(Math.Abs(p.X), Math.Abs(p.Y)));
            }
        }

        // Room for the frame around the tips.
        return reach + 0.5;
    }

    private static Vector Snap(Vector v) => new(SnapComponent(v.X), SnapComponent(v.Y));

    private static double SnapComponent(double c) =>
        Math.Abs(c) < 1e-9 ? 0 : Math.Abs(Math.Abs(c) - 1) < 1e-9 ? Math.Sign(c) : c;

    /// <summary>Builds a closed polygon geometry.</summary>
    public static StreamGeometry Polygon(IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], isFilled: true, isClosed: true);
            ctx.PolyLineTo(points.Skip(1).ToArray(), isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        return geometry;
    }
}
