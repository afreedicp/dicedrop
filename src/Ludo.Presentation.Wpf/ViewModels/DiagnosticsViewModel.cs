using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Ludo.Presentation.Wpf.Presenters;

namespace Ludo.Presentation.Wpf.ViewModels;

/// <summary>The network log panel: what discovery and the network did, to find out why PCs don't connect.</summary>
public sealed class DiagnosticsViewModel : ObservableObject
{
    private readonly WpfDiagnosticLog _log;
    private bool _isOpen;

    internal DiagnosticsViewModel(WpfDiagnosticLog log, Action<Exception> onError)
    {
        _log = log;
        Lines = new ReadOnlyObservableCollection<string>(log.Lines);
        ToggleCommand = new AsyncCommand(_ => { IsOpen = !IsOpen; return Task.CompletedTask; }, onError);
        CopyCommand = new AsyncCommand(_ => { Clipboard.SetText(string.Join(Environment.NewLine, Lines)); return Task.CompletedTask; }, onError);
        ClearCommand = new AsyncCommand(_ => { _log.Lines.Clear(); return Task.CompletedTask; }, onError);
    }

    public ReadOnlyObservableCollection<string> Lines { get; }

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetProperty(ref _isOpen, value);
    }

    public ICommand ToggleCommand { get; }

    /// <summary>Puts the whole log on the clipboard, so it can be compared with the other PC's.</summary>
    public ICommand CopyCommand { get; }

    public ICommand ClearCommand { get; }
}
