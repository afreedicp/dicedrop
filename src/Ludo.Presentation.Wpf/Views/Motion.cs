using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ludo.Presentation.Wpf.Views;

/// <summary>
/// Attached animations for XAML: <c>Enter</c> fades and lifts an element in each time it becomes visible,
/// <c>Pulse</c> breathes its opacity for as long as it is visible. Both stay still when Windows has animations turned off.
/// </summary>
public static class Motion
{
    public static readonly DependencyProperty EnterProperty = DependencyProperty.RegisterAttached(
        "Enter", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnEnterChanged));

    /// <summary>How far below its place the element starts, in pixels. Negative values drop it in from above.</summary>
    public static readonly DependencyProperty EnterOffsetProperty = DependencyProperty.RegisterAttached(
        "EnterOffset", typeof(double), typeof(Motion), new PropertyMetadata(10.0));

    public static readonly DependencyProperty PulseProperty = DependencyProperty.RegisterAttached(
        "Pulse", typeof(bool), typeof(Motion), new PropertyMetadata(false, OnPulseChanged));

    private static readonly Duration EnterDuration = TimeSpan.FromMilliseconds(260);
    private static readonly IEasingFunction EaseOut = Frozen(new CubicEase { EasingMode = EasingMode.EaseOut });

    public static bool GetEnter(DependencyObject element) => (bool)element.GetValue(EnterProperty);

    public static void SetEnter(DependencyObject element, bool value) => element.SetValue(EnterProperty, value);

    public static double GetEnterOffset(DependencyObject element) => (double)element.GetValue(EnterOffsetProperty);

    public static void SetEnterOffset(DependencyObject element, double value) => element.SetValue(EnterOffsetProperty, value);

    public static bool GetPulse(DependencyObject element) => (bool)element.GetValue(PulseProperty);

    public static void SetPulse(DependencyObject element, bool value) => element.SetValue(PulseProperty, value);

    private static bool Animate => SystemParameters.ClientAreaAnimation;

    private static void OnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.IsVisibleChanged -= OnEnterVisibleChanged;
        if ((bool)e.NewValue)
        {
            element.IsVisibleChanged += OnEnterVisibleChanged;
        }
    }

    private static void OnEnterVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not UIElement element || !(bool)e.NewValue || !Animate)
        {
            return;
        }

        var shift = new TranslateTransform();
        element.RenderTransform = shift;
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, EnterDuration) { EasingFunction = EaseOut });
        shift.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(GetEnterOffset(element), 0, EnterDuration) { EasingFunction = EaseOut });
    }

    private static void OnPulseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        element.IsVisibleChanged -= OnPulseVisibleChanged;
        if ((bool)e.NewValue)
        {
            element.IsVisibleChanged += OnPulseVisibleChanged;
            UpdatePulse(element);
        }
        else
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
        }
    }

    private static void OnPulseVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => UpdatePulse((UIElement)sender);

    private static void UpdatePulse(UIElement element)
    {
        if (!element.IsVisible || !Animate)
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            return;
        }

        element.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(900))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = Frozen(new SineEase { EasingMode = EasingMode.EaseInOut }),
            });
    }

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
