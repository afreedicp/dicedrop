using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ludo.Presentation.Wpf.Views;

public partial class SearchingView : UserControl
{
    public SearchingView()
    {
        InitializeComponent();
        Loaded += (_, _) => StartSonar();
    }

    /// <summary>Starts the three rings a third of a cycle apart, so a new one leaves the dice as the oldest fades.</summary>
    private void StartSonar()
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        var sonar = (Storyboard)Resources["Sonar"];
        var rings = new[] { Ring0, Ring1, Ring2 };
        for (var i = 0; i < rings.Length; i++)
        {
            // Each ring needs its own transform: the style's one is shared and frozen.
            rings[i].RenderTransform = new ScaleTransform(0.3, 0.3);
            var ring = sonar.Clone();
            ring.BeginTime = TimeSpan.FromTicks(sonar.Duration.TimeSpan.Ticks * i / rings.Length);
            ring.Begin(rings[i], isControllable: false);
        }
    }
}
