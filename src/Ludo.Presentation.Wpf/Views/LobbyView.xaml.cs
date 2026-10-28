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
}
