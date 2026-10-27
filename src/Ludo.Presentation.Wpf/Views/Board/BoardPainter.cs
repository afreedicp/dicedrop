using System.Windows;
using System.Windows.Media;
using Ludo.Application.Session;

namespace Ludo.Presentation.Wpf.Views.Board;

/// <summary>
/// Paints everything on the board that does not move: frame, track, yards, home columns, safe squares and
/// the centre. The result is frozen and cached, so each animation frame only has to draw the pawns.
/// </summary>
internal static class BoardPainter
{
    private static readonly Brush FrameFill = Frozen(new SolidColorBrush(Color.FromRgb(0x23, 0x27, 0x34)));
    private static readonly Brush ShadowFill = Frozen(new SolidColorBrush(Color.FromArgb(0x38, 0, 0, 0)));
    private static readonly Brush GridFill = Frozen(new SolidColorBrush(Color.FromRgb(0xBD, 0xB8, 0xAD)));
    private static readonly Brush SquareFill = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0xFE, 0xFA)));
    private static readonly Brush StarFill = Frozen(new SolidColorBrush(Color.FromRgb(0x9A, 0x96, 0x8C)));
    private static readonly Brush YardInnerFill = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0xFE, 0xFA)));
    private static readonly Brush SlotShade = Frozen(new SolidColorBrush(Color.FromArgb(0x30, 0, 0, 0)));

    public static Drawing Paint(BoardGeometry geometry, GameStateView game)
    {
        var seatedArms = game.Seats.Where(s => s.Standing != SeatStanding.Left).Select(s => s.Arm).ToHashSet();
        var playedArms = game.Seats.Select(s => s.Arm).ToHashSet();
        Brush Fill(int arm) => playedArms.Contains(arm) && seatedArms.Contains(arm)
            ? SeatPalette.Brush(arm)
            : SeatPalette.MutedBrush(arm);

        var cell = geometry.Cell;
        var pixel = 1 / geometry.PixelsPerDip;
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            PaintFrame(dc, geometry);

            // Grid lines are the gaps between inset squares, so they stay crisp at any size.
            for (var arm = 0; arm < geometry.ArmCount; arm++)
            {
                var (outbound, inbound) = geometry.InnerEdge(arm);
                var length = geometry.Out(arm) * cell * 6;
                dc.DrawGeometry(GridFill, null, BoardGeometry.Polygon([outbound, inbound, inbound + length, outbound + length]));
            }

            var trackLength = game.Board.MainTrackLength;
            for (var square = 0; square < trackLength; square++)
            {
                var quad = geometry.TrackSquare(square);
                var arm = geometry.ArmOfSquare(square);
                var isStart = geometry.IsStartSquare(square);
                dc.DrawGeometry(isStart ? Fill(arm) : SquareFill, null, BoardGeometry.Polygon(quad.Corners(pixel)));
                if (isStart)
                {
                    DrawStar(dc, quad.Center, cell * 0.34, Brushes.White, null);
                }
                else if (geometry.IsStarSquare(square))
                {
                    DrawStar(dc, quad.Center, cell * 0.34, null, new Pen(StarFill, Math.Max(cell * 0.07, pixel)) { LineJoin = PenLineJoin.Round });
                }
            }

            for (var arm = 0; arm < geometry.ArmCount; arm++)
            {
                for (var step = 0; step < game.Board.HomeColumnLength; step++)
                {
                    dc.DrawGeometry(Fill(arm), null, BoardGeometry.Polygon(geometry.HomeSquare(arm, step).Corners(pixel)));
                }

                DrawArrow(dc, geometry.TurnSquare(arm), -geometry.Out(arm), Fill(arm), cell);
                PaintYard(dc, geometry, arm, Fill(arm), cell);
                dc.DrawGeometry(Fill(arm), null, BoardGeometry.Polygon(geometry.HomeTriangle(arm)));
            }

            // Fine seams between the centre triangles, like the printed board.
            var seam = new Pen(Brushes.White, Math.Max(cell * 0.05, pixel));
            for (var arm = 0; arm < geometry.ArmCount; arm++)
            {
                dc.DrawLine(seam, geometry.Center, geometry.InnerEdge(arm).Outbound);
            }
        }

        group.Freeze();
        return group;
    }

    private static void PaintFrame(DrawingContext dc, BoardGeometry geometry)
    {
        var cell = geometry.Cell;
        if (geometry.IsClassic)
        {
            var board = geometry.Bounds;
            var frame = board;
            frame.Inflate(cell * 0.22, cell * 0.22);
            var shadow = frame;
            shadow.Offset(0, cell * 0.18);
            dc.DrawRoundedRectangle(ShadowFill, null, shadow, cell * 0.45, cell * 0.45);
            dc.DrawRoundedRectangle(FrameFill, null, frame, cell * 0.45, cell * 0.45);
            dc.DrawRectangle(GridFill, null, board);
            return;
        }

        // Bigger boards are star shaped: the frame hugs every arm and yard rather than boxing the star in.
        var outline = new GeometryGroup { FillRule = FillRule.Nonzero };
        for (var arm = 0; arm < geometry.ArmCount; arm++)
        {
            var (outbound, inbound) = geometry.InnerEdge(arm);
            var length = geometry.Out(arm) * cell * 6;
            outline.Children.Add(BoardGeometry.Polygon([outbound, inbound, inbound + length, outbound + length]));
            outline.Children.Add(BoardGeometry.Polygon(geometry.Yard(arm)));
            outline.Children.Add(BoardGeometry.Polygon(geometry.HomeTriangle(arm)));
        }

        var border = outline.GetWidenedPathGeometry(new Pen(FrameFill, cell * 0.44) { LineJoin = PenLineJoin.Round });
        var shape = Geometry.Combine(border, outline, GeometryCombineMode.Union, null);
        var shapeShadow = shape.Clone();
        shapeShadow.Transform = new TranslateTransform(0, cell * 0.18);
        dc.DrawGeometry(ShadowFill, null, shapeShadow);
        dc.DrawGeometry(FrameFill, null, shape);
    }

    private static void PaintYard(DrawingContext dc, BoardGeometry geometry, int arm, Brush fill, double cell)
    {
        dc.DrawGeometry(fill, null, BoardGeometry.Polygon(geometry.Yard(arm)));

        // The white inner area has rounded corners: a slightly smaller shape stroked with a round join.
        var round = cell * 0.45;
        var inner = BoardGeometry.Polygon(geometry.Yard(arm, (4 - (round / cell)) / 6));
        dc.DrawGeometry(YardInnerFill, new Pen(YardInnerFill, round) { LineJoin = PenLineJoin.Round }, inner);

        var slotRadius = cell * 0.4;
        for (var slot = 0; slot < 4; slot++)
        {
            var center = geometry.YardSlot(arm, slot);
            dc.DrawEllipse(SlotShade, null, center + new Vector(0, cell * 0.04), slotRadius, slotRadius);
            dc.DrawEllipse(fill, null, center, slotRadius, slotRadius);
            dc.DrawEllipse(Brushes.White, null, center, slotRadius * 0.55, slotRadius * 0.55);
            dc.DrawEllipse(fill, null, center, slotRadius * 0.4, slotRadius * 0.4);
        }
    }

    /// <summary>A chevron on the arm's tip square, pointing into its home column.</summary>
    private static void DrawArrow(DrawingContext dc, Quad square, Vector inward, Brush fill, double cell)
    {
        var side = new Vector(-inward.Y, inward.X);
        var tip = square.Center + (inward * cell * 0.3);
        var back = square.Center - (inward * cell * 0.2);
        var notch = square.Center - (inward * cell * 0.02);
        dc.DrawGeometry(fill, null, BoardGeometry.Polygon(
        [
            tip,
            back + (side * cell * 0.3),
            notch,
            back - (side * cell * 0.3),
        ]));
    }

    private static void DrawStar(DrawingContext dc, Point center, double radius, Brush? fill, Pen? pen)
    {
        var points = new Point[10];
        for (var i = 0; i < 10; i++)
        {
            var r = i % 2 == 0 ? radius : radius * 0.45;
            var angle = (-Math.PI / 2) + (i * Math.PI / 5);
            points[i] = new Point(center.X + (r * Math.Cos(angle)), center.Y + (r * Math.Sin(angle)));
        }

        dc.DrawGeometry(fill, pen, BoardGeometry.Polygon(points));
    }

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
