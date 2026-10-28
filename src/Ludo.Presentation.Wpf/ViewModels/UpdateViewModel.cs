using System.Windows.Input;
using Ludo.Application.UseCases;
using Ludo.Presentation.Wpf.Shell;

namespace Ludo.Presentation.Wpf.ViewModels;

/// <summary>The "update available" banner: checks the release feed now and then, and opens the release page on click.</summary>
public sealed class UpdateViewModel : ObservableObject, IDisposable
{
    // The game often sits in the tray for days, so look again every few hours, not only at startup.
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly CheckForUpdateUseCase _check;
    private readonly CancellationTokenSource _stop = new();
    private Uri? _page;
    private string? _latestTag;
    private bool _dismissed;

    internal UpdateViewModel(CheckForUpdateUseCase check, Action<Exception> onError)
    {
        _check = check;
        OpenCommand = new AsyncCommand(_ => { Open(); return Task.CompletedTask; }, onError);
        DismissCommand = new AsyncCommand(_ => { Dismissed = true; return Task.CompletedTask; }, onError);
    }

    public string CurrentVersion => $"v{_check.CurrentVersion}";

    public string? LatestTag
    {
        get => _latestTag;
        private set
        {
            if (SetProperty(ref _latestTag, value))
            {
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public bool IsVisible => LatestTag is not null && !Dismissed;

    public ICommand OpenCommand { get; }

    /// <summary>Hides the banner until a release newer than the dismissed one appears.</summary>
    public ICommand DismissCommand { get; }

    private bool Dismissed
    {
        get => _dismissed;
        set
        {
            if (SetProperty(ref _dismissed, value))
            {
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    /// <summary>Runs on the UI thread until disposed; every result is applied there.</summary>
    public async void Start()
    {
        if (!_check.IsEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        try
        {
            do
            {
                var release = await _check.ExecuteAsync(_stop.Token);
                if (release is not null && release.Tag != LatestTag)
                {
                    _page = release.Page;
                    LatestTag = release.Tag;
                    Dismissed = false;
                }
            }
            while (await timer.WaitForNextTickAsync(_stop.Token));
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        // Cancelled but not disposed: the check loop may still be reading the token as it unwinds.
        _stop.Cancel();
    }

    private void Open()
    {
        if (_page is not null)
        {
            ProjectLinks.OpenInBrowser(_page);
        }
    }
}
