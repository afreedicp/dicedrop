using System.Windows;
using System.Windows.Controls;

namespace Ludo.Presentation.Wpf.Views;

public partial class LobbyView : UserControl
{
    public LobbyView() => InitializeComponent();

    private void OnNameBoxVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (NameBox.IsVisible)
        {
            NameBox.Focus();
            NameBox.SelectAll();
        }
    }

    /// <summary>Follows new chat lines, unless the user has scrolled up to read older ones.</summary>
    private void OnChatScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange > 0 && e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - e.ExtentHeightChange - 1)
        {
            ChatScroller.ScrollToEnd();
        }
    }
}
