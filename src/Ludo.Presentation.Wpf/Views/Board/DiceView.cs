using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ludo.Presentation.Wpf.Views.Board;

/// <summary>
/// A drawn die. Every new roll tumbles it for a moment (random faces, a wobble and a bounce) before it
/// settles on the result; while it is your turn to roll it bobs and glows in your colour. Clicking it runs
/// <see cref="Command"/>.
/// </summary>
public sealed class DiceView : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(int?), typeof(DiceView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RollCountProperty = DependencyProperty.Register(
        nameof(RollCount), typeof(int), typeof(DiceView), new PropertyMetadata(0, OnRollCountChanged));

    public static readonly DependencyProperty IsRollingProperty = DependencyProperty.Register(
        nameof(IsRolling), typeof(bool), typeof(DiceView), new PropertyMetadata(false, OnAnimationInputChanged));

    public static readonly DependencyProperty IsReadyProperty = DependencyProperty.Register(
        nameof(IsReady), typeof(bool), typeof(DiceView), new PropertyMetadata(false, OnAnimationInputChanged));

    public static readonly DependencyProperty TintProperty = DependencyProperty.Register(
        nameof(Tint), typeof(Brush), typeof(DiceView), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(DiceView));

    private const double TumbleSeconds = 0.7;
    private const double SettleSeconds = 0.18;

    private static readonly Brush ShadowBrush = Frozen(new SolidColorBrush(Color.FromArgb(0x50, 0, 0, 0)));
    private static readonly Brush PipBrush = Frozen(new SolidColorBrush(Color.FromRgb(0x1E, 0x1F, 0x26)));
    private static readonly Brush AcePipBrush = Frozen(new SolidColorBrush(Color.FromRgb(0xD9, 0x2B, 0x2B)));
    private static readonly Pen EdgePen = Frozen(new Pen(new SolidColorBrush(Color.FromRgb(0xC9, 0xC4, 0xB8)), 1));
    private static readonly Brush FaceBrush = Frozen(new LinearGradientBrush(
        Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0xE6, 0xE2, 0xD8), new Point(0.2, 0), new Point(0.8, 1)));

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Random _random = new();
    private double _tumbleStart = double.NegativeInfinity;
    private double _lastFaceChange;
    private int _shownFace = 6;
    private bool _ticking;

    public DiceView()
    {
        Cursor = Cursors.Hand;
        Loaded += (_, _) => UpdateTicking();
        Unloaded += (_, _) => SetTicking(false);
    }

    public int? Value
    {
        get => (int?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Rolls so far; each increase plays the tumble, even when the same face comes up again.</summary>
    public int RollCount
    {
        get => (int)GetValue(RollCountProperty);
        set => SetValue(RollCountProperty, value);
    }

    /// <summary>The roll is under way but its result is not known yet: keep tumbling.</summary>
    public bool IsRolling
    {
        get => (bool)GetValue(IsRollingProperty);
        set => SetValue(IsRollingProperty, value);
    }

    /// <summary>It is our turn to roll.</summary>
    public bool IsReady
    {
        get => (bool)GetValue(IsReadyProperty);
        set => SetValue(IsReadyProperty, value);
    }

    public Brush Tint
    {
        get => (Brush)GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private double Now => _clock.Elapsed.TotalSeconds;

    private bool IsTumbling => IsRolling || Now - _tumbleStart < TumbleSeconds;

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (Command?.CanExecute(null) == true)
        {
            Command.Execute(null);
            e.Handled = true;
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        var now = Now;
        var side = Math.Min(RenderSize.Width, RenderSize.Height) * 0.72;
        if (side <= 4)
        {
            return;
        }

        var center = new Point(RenderSize.Width / 2, RenderSize.Height / 2);
        double angle = 0, scale = 1, hop = 0, squash = 0;
        var face = Value ?? 6;

        if (IsTumbling)
        {
            var t = now - _tumbleStart;
            if (now - _lastFaceChange > 0.07)
            {
                _shownFace = 1 + ((_shownFace + _random.Next(1, 6)) % 6);
                _lastFaceChange = now;
            }

            face = _shownFace;
            var decay = IsRolling ? 1 : Math.Max(0, 1 - (t / TumbleSeconds));
            angle = Math.Sin(t * 26) * 28 * decay;
            hop = Math.Abs(Math.Sin(t * 13)) * side * 0.18 * decay;
            scale = 1 + (0.06 * decay);
        }
        else
        {
            var sinceEnd = now - (_tumbleStart + TumbleSeconds);
            if (sinceEnd < SettleSeconds)
            {
                squash = 1 - (sinceEnd / SettleSeconds);
            }
            else if (IsReady)
            {
                hop = Math.Abs(Math.Sin(now * 4.2)) * side * 0.07;
            }
        }

        if (IsReady && !IsTumbling)
        {
            var pulse = 0.5 + (0.5 * Math.Sin(now * 5));
            dc.PushOpacity(0.35 + (0.45 * pulse));
            var glow = side * (0.62 + (0.06 * pulse));
            dc.DrawRoundedRectangle(null, new Pen(Tint, side * 0.08), new Rect(center.X - glow, center.Y - glow, glow * 2, glow * 2), side * 0.28, side * 0.28);
            dc.Pop();
        }

        // Shadow stays on the ground while the die jumps.
        var shadowWidth = side * (0.5 - (hop / side * 0.4));
        dc.DrawEllipse(ShadowBrush, null, new Point(center.X, center.Y + (side * 0.55)), shadowWidth, side * 0.09);

        var transform = Matrix.Identity;
        transform.ScaleAt(scale * (1 + (0.1 * squash)), scale * (1 - (0.12 * squash)), 0, side / 2);
        transform.Rotate(angle);
        transform.Translate(center.X, center.Y - hop);
        dc.PushTransform(new MatrixTransform(transform));
        var body = new Rect(-side / 2, -side / 2, side, side);
        dc.DrawRoundedRectangle(FaceBrush, EdgePen, body, side * 0.2, side * 0.2);
        DrawPips(dc, face, side);
        dc.Pop();
    }

    private static void DrawPips(DrawingContext dc, int face, double side)
    {
        var r = side * 0.085;
        var o = side * 0.25;
        var pips = face switch
        {
            1 => new[] { (0.0, 0.0) },
            2 => [(-1, -1), (1, 1)],
            3 => [(-1, -1), (0, 0), (1, 1)],
            4 => [(-1, -1), (1, -1), (-1, 1), (1, 1)],
            5 => [(-1, -1), (1, -1), (0, 0), (-1, 1), (1, 1)],
            _ => [(-1, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (1, 1)],
        };

        var brush = face == 1 ? AcePipBrush : PipBrush;
        var radius = face == 1 ? r * 1.45 : r;
        foreach (var (x, y) in pips)
        {
            dc.DrawEllipse(brush, null, new Point(x * o, y * o), radius, radius);
        }
    }

    private static void OnRollCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dice = (DiceView)d;
        if ((int)e.NewValue > (int)e.OldValue)
        {
            dice._tumbleStart = dice.Now;
        }

        dice.UpdateTicking();
    }

    private static void OnAnimationInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dice = (DiceView)d;
        if (e.Property == IsRollingProperty && (bool)e.NewValue)
        {
            dice._tumbleStart = dice.Now;
        }

        dice.UpdateTicking();
    }

    private void UpdateTicking() => SetTicking(IsLoaded);

    private void SetTicking(bool on)
    {
        if (on == _ticking)
        {
            return;
        }

        _ticking = on;
        if (on)
        {
            CompositionTarget.Rendering += OnFrame;
        }
        else
        {
            CompositionTarget.Rendering -= OnFrame;
        }
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        var now = Now;
        if (IsReady || IsTumbling || now - (_tumbleStart + TumbleSeconds) < SettleSeconds + 0.05)
        {
            InvalidateVisual();
        }
    }

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
