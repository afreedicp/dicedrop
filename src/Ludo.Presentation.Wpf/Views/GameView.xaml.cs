using System.Windows.Controls;
using System.Windows.Input;
using Ludo.Presentation.Wpf.ViewModels;

namespace Ludo.Presentation.Wpf.Views;

public partial class GameView : UserControl
{
    public GameView()
    {
        InitializeComponent();

        // Take keyboard focus so Space/Enter roll without clicking first.
        Loaded += (_, _) =>
        {
            Focusable = true;
            Focus();
        };
    }

    /// <summary>Space or Enter rolls, except while typing in the chat box.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.OriginalSource is TextBox || e.Key is not (Key.Space or Key.Enter)
            || DataContext is not GameViewModel { RollCommand: var roll } || !roll.CanExecute(null))
        {
            return;
        }

        roll.Execute(null);
        e.Handled = true;
    }
}
