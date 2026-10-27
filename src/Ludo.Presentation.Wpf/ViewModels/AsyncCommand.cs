using System.Windows.Input;

namespace Ludo.Presentation.Wpf.ViewModels;

/// <summary>Runs an async action, disables itself while running, and reports failures instead of crashing.</summary>
public sealed class AsyncCommand(Func<object?, Task> execute, Action<Exception> onError, Func<object?, bool>? canExecute = null)
    : ICommand
{
    private bool _running;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_running && (canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _running = true;
        RaiseCanExecuteChanged();
        try
        {
            await execute(parameter);
        }
        catch (Exception ex)
        {
            onError(ex);
        }
        finally
        {
            _running = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
