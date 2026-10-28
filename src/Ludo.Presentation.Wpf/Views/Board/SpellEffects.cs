using System.Windows;
using System.Windows.Media;

namespace Ludo.Presentation.Wpf.Views.Board;

/// <summary>The element a seat's colour stands for. Its spells look and act according to it.</summary>
internal enum Element
{
    /// <summary>Red: fire engulfs the victim and burns it away to ash.</summary>
    Fire,

    /// <summary>Green: vines burst from the ground and drag the victim underground.</summary>
    Forest,

    /// <summary>Yellow: a small sun hovers over the victim and burns it to ash.</summary>
    Light,

    /// <summary>Blue: a huge wave sweeps the victim away.</summary>
    Water,
}

/// <summary>How a struck wizard moves during the impact: how far it topples, fades, sinks and flattens.</summary>
/// <param name="Tilt">Toppling angle in radians, negative to the left.</param>
/// <param name="Alpha">Opacity.</param>
/// <param name="Sink">How far it is pulled into the ground, in cells.</param>
/// <param name="Squash">Extra flattening, 0 at rest.</param>
/// <param name="Pinch">How narrowly the wizard is squeezed, 0 at rest and 1 crushed.</param>
/// <param name="Drift">How far it is swept sideways, in cells.</param>
internal readonly record struct ImpactPose(double Tilt, double Alpha, double Sink, double Squash, double Pinch = 0, double Drift = 0);

/// <summary>When each stage of a spell starts and ends, as shares of its length, and how long it lasts in seconds.</summary>
internal readonly record struct SpellTiming(double Seconds, double ChargeEnd, double FlightEnd, double ImpactEnd, double AftermathEnd);

/// <summary>
/// Draws each element's spell: the charge in the caster's hand, the flight to the victim, the impact that
/// strikes it, and the aftermath that lingers on its square. Colours follow the element, not the seat.
/// </summary>
internal static class SpellEffects
{
    private const int Rays = 8;
    private const int VineCount = 3;
    private const int VineSteps = 36;
    private const int FlameCount = 7;

    private static readonly Brush Bark = Frozen(new SolidColorBrush(Color.FromRgb(0x4A, 0x32, 0x1C)));
    private static readonly Brush FlameRed = Frozen(new SolidColorBrush(Color.FromRgb(0xD7, 0x26, 0x1E)));
    private static readonly Brush FlameOrange = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x1A)));
    private static readonly Brush FlameYellow = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0xE2, 0x7A)));
    private static readonly Brush Scorch = Frozen(new SolidColorBrush(Color.FromArgb(0xB0, 0x3A, 0x0E, 0x06)));
    private static readonly Brush WaveBlue = Frozen(new SolidColorBrush(Color.FromRgb(0x2A, 0x7F, 0xD6)));
    private static readonly Brush SunGlow = Orb(Colors.White, Color.FromRgb(0xFF, 0xD8, 0x5A));

    private static readonly Brush FireOrb = Orb(Color.FromRgb(0xFF, 0xE2, 0x7A), Color.FromRgb(0xE0, 0x28, 0x1E));
    private static readonly Brush WaterOrb = Orb(Color.FromRgb(0xE6, 0xF9, 0xFF), Color.FromRgb(0x3F, 0xA9, 0xF5));
    private static readonly Brush LightOrb = Orb(Colors.White, Color.FromRgb(0xFF, 0xE0, 0x66));
    private static readonly Brush ForestOrb = Orb(Color.FromRgb(0xE6, 0xFF, 0xD0), Color.FromRgb(0x2E, 0x9E, 0x4A));

    private static readonly Brush Ash = Frozen(new SolidColorBrush(Color.FromRgb(0x3A, 0x2A, 0x22)));
    private static readonly Brush Ember = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0x8A, 0x1F)));
    private static readonly Brush WaterBlue = Frozen(new SolidColorBrush(Color.FromRgb(0x3F, 0xA9, 0xF5)));
    private static readonly Brush Foam = Frozen(new SolidColorBrush(Color.FromRgb(0xDF, 0xF4, 0xFF)));
    private static readonly Brush Gold = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0xD8, 0x5A)));
    private static readonly Brush Leaf = Frozen(new SolidColorBrush(Color.FromRgb(0x3C, 0xB0, 0x5A)));
    private static readonly Brush LeafDark = Frozen(new SolidColorBrush(Color.FromRgb(0x1F, 0x6B, 0x35)));
    private static readonly Brush Light = Frozen(new SolidColorBrush(Colors.White));

    /// <summary>The element of a seat's colour: red, green, yellow and blue are fire, forest, light and water.</summary>
    public static Element ElementOf(int arm) => (Element)(((arm % 4) + 4) % 4);

    /// <summary>When each stage of each element's spell starts and ends.</summary>
    public static SpellTiming TimingOf(Element element) => element switch
    {
        Element.Forest => new SpellTiming(2.4, 0.1, 0.15, 0.6, 0.85),
        Element.Fire => new SpellTiming(2.4, 0.15, 0.22, 0.6, 0.8),
        Element.Water => new SpellTiming(2.4, 0.15, 0.22, 0.7, 0.9),
        Element.Light => new SpellTiming(2.4, 0.15, 0.3, 0.7, 0.9),
        _ => throw new ArgumentOutOfRangeException(nameof(element)),
    };

    /// <summary>The charge gathering in the caster's hand, growing over <paramref name="t"/> (0 to 1).</summary>
    public static void DrawCharge(DrawingContext dc, Element element, Point hand, double size, double t)
    {
        var radius = size * (0.04 + (0.2 * t));
        dc.DrawEllipse(OrbFor(element), null, hand, radius, radius);

        switch (element)
        {
            case Element.Fire:
                for (var i = 0; i < 3; i++)
                {
                    var angle = (i * 2.1) + (t * 6);
                    var spark = hand + (new Vector(Math.Cos(angle), Math.Sin(angle)) * radius * 1.4);
                    dc.DrawEllipse(Ember, null, spark, size * 0.03, size * 0.03);
                }

                break;
            case Element.Forest:
                for (var i = 0; i < 3; i++)
                {
                    var angle = (i * 2.1) + (t * 5);
                    var leaf = hand + (new Vector(Math.Cos(angle), Math.Sin(angle)) * size * 0.22 * t);
                    dc.DrawEllipse(i % 2 == 0 ? Leaf : LeafDark, null, leaf, size * 0.05, size * 0.025);
                }

                break;
            case Element.Light:
                for (var i = 0; i < Rays; i++)
                {
                    var angle = i * 2 * Math.PI / Rays;
                    var direction = new Vector(Math.Cos(angle), Math.Sin(angle));
                    dc.DrawLine(new Pen(Gold, size * 0.03) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, hand + (direction * radius * 1.2), hand + (direction * (radius + (size * 0.2 * t))));
                }

                break;
            case Element.Water:
                for (var i = 0; i < 2; i++)
                {
                    var angle = (i * Math.PI) + (t * 7);
                    var drop = hand + (new Vector(Math.Cos(angle), Math.Sin(angle) * 0.5) * radius * 1.5);
                    dc.DrawEllipse(Foam, null, drop, size * 0.03, size * 0.03);
                }

                break;
        }
    }

    /// <summary>
    /// The spell flying from <paramref name="from"/> to <paramref name="to"/>, at <paramref name="t"/> (0 to 1).
    /// For light, <paramref name="from"/> is the sky above the victim, where the sun starts its descent.
    /// </summary>
    public static void DrawProjectile(DrawingContext dc, Element element, Point from, Point to, double size, double t)
    {
        var tip = Lerp(from, to, t);
        switch (element)
        {
            case Element.Fire:
                for (var i = 3; i >= 1; i--)
                {
                    dc.DrawEllipse(Ember, null, Lerp(from, to, Math.Max(0, t - (i * 0.08))), size * (0.22 - (i * 0.04)), size * (0.22 - (i * 0.04)));
                }

                dc.DrawEllipse(FireOrb, null, tip, size * 0.22, size * 0.22);
                break;
            case Element.Water:
                for (var i = 1; i <= 3; i++)
                {
                    dc.DrawEllipse(WaterBlue, null, Lerp(from, to, Math.Max(0, t - (i * 0.07))), size * 0.05, size * 0.05);
                }

                dc.DrawEllipse(WaterOrb, null, tip, size * 0.2, size * 0.2);
                break;
            case Element.Light:
                // A small sun swoops down from the sky to hover just above the victim, its beam lancing onto it.
                var hover = to - new Vector(0, size * 0.9);
                var sun = Lerp(from, hover, Ease(t));
                DrawSun(dc, sun, size * 0.4, t);
                dc.DrawLine(new Pen(Gold, size * 0.12 * t) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, sun, to);
                dc.DrawLine(new Pen(Light, size * 0.04 * t) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, sun, to);
                break;
            case Element.Forest:
                DrawVine(dc, from, tip, size, 0.12);
                dc.DrawEllipse(Leaf, null, tip, size * 0.09, size * 0.05);
                break;
        }
    }

    /// <summary>What a struck wizard does during the impact, at <paramref name="t"/> (0 to 1).</summary>
    public static ImpactPose Impact(Element element, double t)
    {
        var eased = Ease(t);
        return element switch
        {
            Element.Fire => new ImpactPose(0, 1 - Ease(Math.Clamp((t - 0.5) / 0.5, 0, 1)), 0, 0.3 * t),
            Element.Water => WaterImpact(t),
            Element.Light => new ImpactPose(0, 1 - Ease(Math.Clamp((t - 0.3) / 0.6, 0, 1)), 0, 0.5 * t),
            Element.Forest => ForestImpact(t),
            _ => throw new ArgumentOutOfRangeException(nameof(element)),
        };
    }

    /// <summary>The impact itself, on the victim's square, at <paramref name="t"/> (0 to 1).</summary>
    public static void DrawImpact(DrawingContext dc, Element element, Point foot, double size, double t)
    {
        var eased = Ease(t);
        switch (element)
        {
            case Element.Fire:
            {
                // The ground under the victim scorches black while the flames rise around it (see DrawImpactFront).
                dc.DrawEllipse(Scorch, null, foot, size * 0.7 * eased, size * 0.28 * eased);
                break;
            }

            case Element.Water:
            {
                // The ground ahead of the wave churns: a splash ring spreads and spray flies out from the square.
                var ring = size * (0.4 + (1.6 * eased));
                dc.DrawEllipse(null, new Pen(Foam, size * 0.07 * (1 - t)), foot, ring, ring * 0.4);
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4;
                    var drop = foot + new Vector(Math.Cos(angle) * size * 1.4 * eased, (Math.Sin(angle) * size * 0.4) - (size * 0.8 * t * (1 - t)));
                    dc.DrawEllipse(Foam, null, drop, size * 0.06, size * 0.06);
                }

                break;
            }

            case Element.Light:
            {
                // The ground beneath the wizard blazes white-gold as the heat builds.
                dc.DrawEllipse(SunGlow, null, foot, size * (0.5 + (0.6 * eased)), size * (0.2 + (0.2 * eased)));
                break;
            }

            case Element.Forest:
            {
                // Roots spread across the square, the vines climb behind the wizard, and leaves are flung out.
                var growth = ForestGrowth(t);
                for (var i = 0; i < 5; i++)
                {
                    var angle = (-Math.PI / 2) + ((i - 2) * 0.6);
                    var tip = foot + (new Vector(Math.Cos(angle), Math.Sin(angle) * 0.4) * size * 0.7 * growth);
                    DrawVine(dc, foot, tip, size, 0.1);
                }

                DrawForestVines(dc, foot, size, t, front: false);
                DrawLeafBurst(dc, foot, size, t);
                break;
            }
        }
    }

    /// <summary>The part of the impact that passes in front of the wizard, drawn over it, at <paramref name="t"/> (0 to 1).</summary>
    public static void DrawImpactFront(DrawingContext dc, Element element, Point foot, double size, double t)
    {
        if (element == Element.Fire)
        {
            DrawFlames(dc, foot, size, t);
        }
        else if (element == Element.Water)
        {
            DrawWave(dc, foot, size, t);
        }
        else if (element == Element.Light)
        {
            DrawSunStrike(dc, foot, size, t);
        }
        else if (element == Element.Forest)
        {
            DrawForestVines(dc, foot, size, t, front: true);
        }
    }

    private static ImpactPose ForestImpact(double t)
    {
        var pinch = ForestPinch(t);
        var sink = 0.9 * Ease(Math.Clamp((t - 0.7) / 0.3, 0, 1));
        var fade = 1 - Ease(Math.Clamp((t - 0.85) / 0.15, 0, 1));
        return new ImpactPose(0.05 * Math.Sin(t * 40) * pinch, fade, sink, 0, pinch);
    }

    /// <summary>How tightly the vines have closed round the wizard: loose at first, crushing by the end.</summary>
    private static double ForestPinch(double t) => Ease(Math.Clamp((t - 0.3) / 0.5, 0, 1));

    /// <summary>How far the vines have grown out of the ground: they rise, then sink back once the wizard is gone.</summary>
    private static double ForestGrowth(double t) => Ease(Math.Clamp(t / 0.4, 0, 1)) * (1 - Ease(Math.Clamp((t - 0.85) / 0.15, 0, 1)));

    /// <summary>
    /// Three thick vines that spiral up the wizard from the ground and tighten as they climb. The half that
    /// passes behind it is drawn before it and the half in front afterwards, so they wrap it.
    /// </summary>
    private static void DrawForestVines(DrawingContext dc, Point foot, double size, double t, bool front)
    {
        var growth = ForestGrowth(t);
        if (growth <= 0)
        {
            return;
        }

        var pinch = ForestPinch(t);
        var radius = size * (0.45 - (0.22 * pinch));
        var height = size * 1.3 * growth;
        var thickness = size * (0.07 + (0.05 * pinch));
        for (var v = 0; v < VineCount; v++)
        {
            var phase = v * 2 * Math.PI / VineCount;
            var points = new Point[VineSteps + 1];
            var depth = new double[VineSteps + 1];
            for (var k = 0; k <= VineSteps; k++)
            {
                var u = (double)k / VineSteps;
                var angle = phase + (u * 3 * Math.PI);
                points[k] = foot + new Vector(Math.Cos(angle) * radius, -u * height);
                depth[k] = Math.Sin(angle);
            }

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                var open = false;
                for (var k = 0; k < VineSteps; k++)
                {
                    if ((depth[k] >= 0) != front)
                    {
                        open = false;
                        continue;
                    }

                    if (!open)
                    {
                        ctx.BeginFigure(points[k], isFilled: false, isClosed: false);
                        open = true;
                    }

                    ctx.LineTo(points[k + 1], isStroked: true, isSmoothJoin: true);
                }
            }

            geometry.Freeze();
            dc.DrawGeometry(null, new Pen(Bark, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round }, geometry);

            for (var k = 0; k < VineSteps; k += 5)
            {
                if ((depth[k] >= 0) != front)
                {
                    continue;
                }

                var leaf = points[k] + new Vector(size * 0.08 * (depth[k] >= 0 ? 1 : -1), 0);
                dc.DrawEllipse(k % 10 == 0 ? Leaf : LeafDark, null, leaf, size * 0.09, size * 0.04);
            }
        }
    }

    /// <summary>Leaves flung out from the square as the vines close, fading as they go.</summary>
    private static void DrawLeafBurst(DrawingContext dc, Point foot, double size, double t)
    {
        var burst = Ease(Math.Clamp((t - 0.6) / 0.25, 0, 1));
        var fade = 1 - Ease(Math.Clamp((t - 0.8) / 0.2, 0, 1));
        if (burst <= 0 || fade <= 0)
        {
            return;
        }

        dc.PushOpacity(fade);
        for (var i = 0; i < 8; i++)
        {
            var angle = i * Math.PI / 4;
            var distance = size * (0.4 + (0.9 * burst));
            var leaf = foot + new Vector(Math.Cos(angle) * distance, (Math.Sin(angle) * distance * 0.5) - (size * 0.8 * burst));
            dc.DrawEllipse(i % 2 == 0 ? Leaf : LeafDark, null, leaf, size * 0.09, size * 0.045);
        }

        dc.Pop();
    }

    /// <summary>
    /// The sun, high over the board: a white-gold disc with a glowing corona and long rays that turn slowly,
    /// swelling from nothing as the spell begins. <paramref name="t"/> runs from 0 to 1 across the flight.
    /// </summary>
    private static void DrawSun(DrawingContext dc, Point centre, double size, double t)
    {
        var radius = size * (0.4 + (0.9 * Ease(t)));
        dc.PushOpacity(Math.Min(1, t * 3));
        dc.DrawEllipse(SunGlow, null, centre, radius * 2.2, radius * 2.2);
        dc.DrawEllipse(Light, null, centre, radius, radius);
        for (var i = 0; i < 12; i++)
        {
            var angle = (i * 2 * Math.PI / 12) + (t * 2);
            var direction = new Vector(Math.Cos(angle), Math.Sin(angle));
            var reach = radius * (1.6 + (0.4 * Math.Sin((t * 10) + i)));
            dc.DrawLine(new Pen(Gold, size * 0.05) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, centre + (direction * radius * 1.1), centre + (direction * reach));
        }

        dc.Pop();
    }

    /// <summary>
    /// The sun's searing heat falling on the wizard: a white-gold blaze over it, with heat shimmers rising
    /// through the light, until the wizard is consumed. Drawn over the wizard.
    /// </summary>
    private static void DrawSunStrike(DrawingContext dc, Point foot, double size, double t)
    {
        var searing = Ease(Math.Clamp((t - 0.2) / 0.4, 0, 1));
        var fade = 1 - Ease(Math.Clamp((t - 0.75) / 0.25, 0, 1));
        if (searing <= 0 || fade <= 0)
        {
            return;
        }

        dc.PushOpacity(fade);
        var centre = foot - new Vector(0, size * 0.6);
        dc.DrawEllipse(SunGlow, null, centre, size * (0.4 + (0.4 * searing)), size * (0.7 + (0.3 * searing)));
        for (var i = 0; i < 6; i++)
        {
            var x = foot.X + ((i - 2.5) * size * 0.22);
            var shimmer = Math.Sin((t * 40) + i) * size * 0.04;
            dc.DrawLine(new Pen(Gold, size * 0.03) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, new Point(x + shimmer, foot.Y), new Point(x - shimmer, foot.Y - (size * 1.3 * searing)));
        }

        dc.Pop();
    }

    /// <summary>What a wizard caught by the wave does: it tumbles, is swept sideways and is gone under the water.</summary>
    private static ImpactPose WaterImpact(double t)
    {
        var sweep = Ease(Math.Clamp((t - 0.1) / 0.5, 0, 1));
        var tumble = Ease(Math.Clamp((t - 0.15) / 0.4, 0, 1));
        var gone = Ease(Math.Clamp((t - 0.45) / 0.35, 0, 1));
        return new ImpactPose(-1.4 * tumble, 1 - gone, 0, 0, 0, 2.2 * sweep);
    }

    /// <summary>
    /// A huge wave sweeps across the square from the left: a towering curling crest with white foam along
    /// its edge, passing over the wizard and fading as it rolls on. Drawn over the wizard.
    /// </summary>
    private static void DrawWave(DrawingContext dc, Point foot, double size, double t)
    {
        var reach = Ease(Math.Clamp(t / 0.25, 0, 1));
        var pass = Ease(Math.Clamp(t / 0.6, 0, 1));
        var fade = 1 - Ease(Math.Clamp((t - 0.6) / 0.4, 0, 1));
        if (reach <= 0 || fade <= 0)
        {
            return;
        }

        var centre = foot.X + (size * (-2.8 + (5.6 * pass)));
        var length = size * 2.4;
        var height = size * 2.0 * reach;
        var baseY = foot.Y + (size * 0.15);
        var crest = new Point(centre + (length * 0.2), baseY - (height * 0.95));
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(centre - length, baseY + (size * 0.5)), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(centre - length, baseY), isStroked: true, isSmoothJoin: true);
            ctx.BezierTo(new Point(centre - (length * 0.5), baseY - (height * 0.6)), new Point(centre - (length * 0.1), baseY - height), crest, isStroked: true, isSmoothJoin: true);
            ctx.BezierTo(new Point(centre + (length * 0.6), baseY - (height * 1.05)), new Point(centre + (length * 0.95), baseY - (height * 0.5)), new Point(centre + (length * 0.7), baseY - (height * 0.2)), isStroked: true, isSmoothJoin: true);
            ctx.BezierTo(new Point(centre + (length * 0.9), baseY - (height * 0.1)), new Point(centre + length, baseY), new Point(centre + length, baseY + (size * 0.5)), isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        dc.PushOpacity(0.9 * fade);
        dc.DrawGeometry(WaveBlue, new Pen(Foam, size * 0.08) { LineJoin = PenLineJoin.Round }, geometry);
        dc.Pop();

        // Spray torn off the crest as it rolls.
        for (var i = 0; i < 5; i++)
        {
            var spray = new Point(crest.X + (size * (0.3 * i)), crest.Y - (size * 0.25 * i * reach));
            dc.PushOpacity(fade);
            dc.DrawEllipse(Foam, null, spray, size * 0.07, size * 0.07);
            dc.Pop();
        }
    }

    /// <summary>
    /// Flames that engulf the wizard: tongues of red, orange and yellow flicker and lick upward, then die down
    /// as the wizard is gone. Drawn over the wizard.
    /// </summary>
    private static void DrawFlames(DrawingContext dc, Point foot, double size, double t)
    {
        var rise = Ease(Math.Clamp(t / 0.35, 0, 1));
        var fade = 1 - Ease(Math.Clamp((t - 0.75) / 0.25, 0, 1));
        if (rise <= 0 || fade <= 0)
        {
            return;
        }

        dc.PushOpacity(fade);
        for (var i = 0; i < FlameCount; i++)
        {
            var spread = (i - ((FlameCount - 1) / 2.0)) / FlameCount;
            var flicker = 0.8 + (0.2 * Math.Sin((t * 30) + (i * 1.7)));
            var height = size * (1.5 - (Math.Abs(spread) * 0.8)) * rise * flicker;
            var width = size * 0.2;
            var sway = Math.Sin((t * 18) + i) * size * 0.08;
            var baseCentre = foot + new Vector(spread * size, size * 0.05);
            dc.DrawGeometry(FlameRed, null, Tongue(baseCentre, width, height, sway));
            dc.DrawGeometry(FlameOrange, null, Tongue(baseCentre, width * 0.65, height * 0.75, sway * 0.7));
            dc.DrawGeometry(FlameYellow, null, Tongue(baseCentre, width * 0.3, height * 0.45, sway * 0.4));
        }

        dc.Pop();
    }

    /// <summary>One tongue of flame, rising from a base point and leaning with <paramref name="sway"/> at its tip.</summary>
    private static Geometry Tongue(Point baseCentre, double width, double height, double sway)
    {
        var tip = baseCentre + new Vector(sway, -height);
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(baseCentre + new Vector(-width, 0), isFilled: true, isClosed: true);
            ctx.BezierTo(baseCentre + new Vector(-width * 1.3, -height * 0.4), baseCentre + new Vector((sway * 0.5) - (width * 0.5), -height * 0.8), tip, isStroked: true, isSmoothJoin: true);
            ctx.BezierTo(baseCentre + new Vector((sway * 0.5) + (width * 0.5), -height * 0.8), baseCentre + new Vector(width * 1.3, -height * 0.4), baseCentre + new Vector(width, 0), isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        return geometry;
    }

    /// <summary>What lingers on the square after the impact, at <paramref name="t"/> (0 to 1).</summary>
    public static void DrawAftermath(DrawingContext dc, Element element, Point foot, double size, double t)
    {
        switch (element)
        {
            case Element.Fire:
                dc.PushOpacity(0.75 * (1 - t));
                for (var i = 0; i < 5; i++)
                {
                    var spread = (i - 2) * 0.2;
                    var centre = foot + new Vector(size * spread * (0.6 + t), -size * t * (0.5 + (0.12 * i)));
                    var radius = size * (0.2 + (0.25 * t));
                    dc.DrawEllipse(Ash, null, centre, radius, radius * 0.85);
                }

                for (var i = 0; i < 6; i++)
                {
                    var ember = foot + new Vector((i - 2.5) * size * 0.18, -size * t * (0.8 + (0.1 * i)));
                    dc.DrawEllipse(Ember, null, ember, size * 0.04, size * 0.04);
                }

                dc.Pop();
                break;
            case Element.Water:
                for (var i = 0; i < 2; i++)
                {
                    var ripple = size * (0.3 + (0.7 * (t - (i * 0.15))));
                    dc.PushOpacity(Math.Max(0, 1 - t));
                    dc.DrawEllipse(null, new Pen(WaterBlue, size * 0.05), foot, ripple, ripple * 0.4);
                    dc.Pop();
                }

                for (var i = 0; i < 3; i++)
                {
                    var drop = foot + new Vector((i - 1) * size * 0.4, (-size * (1 - t) * (1 + (0.3 * i))) + (size * 0.2));
                    dc.PushOpacity(1 - t);
                    dc.DrawEllipse(WaterBlue, null, drop, size * 0.05, size * 0.07);
                    dc.Pop();
                }

                break;
            case Element.Light:
                // A grey heap of ash settles on the square while flakes drift down from the sun's heat.
                dc.DrawEllipse(Ash, null, foot, size * (0.25 + (0.35 * t)), size * (0.1 + (0.1 * t)));
                for (var i = 0; i < 7; i++)
                {
                    var flake = foot + new Vector((i - 3) * size * 0.2, (-size * 1.2 * (1 - t)) + (size * 0.1 * (i % 3)));
                    dc.PushOpacity(0.8 * (1 - (t * 0.5)));
                    dc.DrawEllipse(Ash, null, flake, size * 0.04, size * 0.025);
                    dc.Pop();
                }

                break;
            case Element.Forest:
                for (var i = 0; i < 6; i++)
                {
                    var angle = (i * Math.PI / 3) + (t * 3);
                    var leaf = foot + new Vector(Math.Cos(angle) * size * (0.3 + (0.5 * t)), (Math.Sin(angle) * size * 0.2) - (size * 0.8 * t));
                    dc.PushOpacity(1 - t);
                    dc.DrawEllipse(i % 2 == 0 ? Leaf : LeafDark, null, leaf, size * 0.08, size * 0.045);
                    dc.Pop();
                }

                break;
        }
    }

    /// <summary>A curling stem from one point to another, with a little wave in it.</summary>
    private static void DrawVine(DrawingContext dc, Point from, Point to, double size, double width)
    {
        var axis = to - from;
        var perpendicular = new Vector(-axis.Y, axis.X);
        if (perpendicular.Length > 0)
        {
            perpendicular.Normalize();
        }

        var curl = perpendicular * (size * 0.18);
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(from, isFilled: false, isClosed: false);
            ctx.BezierTo(from + (axis * 0.3) + curl, from + (axis * 0.7) - curl, to, isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        dc.DrawGeometry(null, new Pen(LeafDark, size * width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, geometry);
    }

    private static Brush OrbFor(Element element) => element switch
    {
        Element.Fire => FireOrb,
        Element.Water => WaterOrb,
        Element.Light => LightOrb,
        Element.Forest => ForestOrb,
        _ => throw new ArgumentOutOfRangeException(nameof(element)),
    };

    private static Brush Orb(Color core, Color edge) => Frozen(new RadialGradientBrush
    {
        GradientStops =
        [
            new GradientStop(core, 0),
            new GradientStop(edge, 0.5),
            new GradientStop(Color.FromArgb(0x00, edge.R, edge.G, edge.B), 1),
        ],
    });

    private static Point Lerp(Point a, Point b, double t) => a + ((b - a) * t);

    private static double Ease(double t) => t * t * (3 - (2 * t));

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
