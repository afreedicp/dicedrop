using System.Collections.Specialized;
using Ludo.Presentation.Wpf.Security;

namespace Ludo.Presentation.Wpf.Views;

public partial class MainWindow : ProtectedWindow
{
    public MainWindow()
    {
        InitializeComponent();

        // The network log follows the newest line.
        ((INotifyCollectionChanged)DiagnosticLines.Items).CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add && DiagnosticLines.Items.Count > 0)
            {
                DiagnosticLines.ScrollIntoView(DiagnosticLines.Items[^1]);
            }
        };
    }
}
