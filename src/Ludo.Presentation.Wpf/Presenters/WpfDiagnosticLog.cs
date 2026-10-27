using System.Collections.ObjectModel;
using System.Windows.Threading;
using Ludo.Application.Abstractions;

namespace Ludo.Presentation.Wpf.Presenters;

/// <summary>
/// Implements the diagnostics port. Lines arrive from any thread and are kept, newest last, in a collection
/// only the UI thread touches. Held in memory only: nothing is written to disk.
/// </summary>
internal sealed class WpfDiagnosticLog(Dispatcher dispatcher) : IDiagnosticLog
{
    private const int MaxLines = 2000;

    public ObservableCollection<string> Lines { get; } = [];

    public void Write(string source, string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff}  {source,-5}  {message}";
        dispatcher.BeginInvoke(() =>
        {
            Lines.Add(line);
            if (Lines.Count > MaxLines)
            {
                Lines.RemoveAt(0);
            }
        });
    }
}
