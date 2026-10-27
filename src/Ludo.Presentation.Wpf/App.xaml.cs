using System.Windows;
using Ludo.Application;
using Ludo.Infrastructure.Crypto;
using Ludo.Infrastructure.Discovery;
using Ludo.Infrastructure.Networking;
using Ludo.Presentation.Wpf.Presenters;
using Ludo.Presentation.Wpf.Security;
using Ludo.Presentation.Wpf.Shell;
using Ludo.Presentation.Wpf.ViewModels;
using Ludo.Presentation.Wpf.Views;

namespace Ludo.Presentation.Wpf;

/// <summary>Composition root: the only place that knows every layer and wires adapters to ports.</summary>
public partial class App : System.Windows.Application
{
    private ProcessWindowGuard? _windowGuard;
    private UdpLobbyDiscovery? _discovery;
    private LudoApplication? _app;
    private ChatToastController? _chatToasts;
    private SingleInstance? _instance;
    private TrayShell? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var startInTray = e.Args.Contains(StartupRegistration.TrayArgument, StringComparer.OrdinalIgnoreCase);
        _instance = SingleInstance.TryClaim(quiet: startInTray);
        if (_instance is null)
        {
            Shutdown();
            return;
        }

        // Protection goes up before any window exists.
        PopupSuppression.Register();
        _windowGuard = ProcessWindowGuard.Install();

        var presenter = new WpfSessionPresenter(Dispatcher);
        var diagnostics = new WpfDiagnosticLog(Dispatcher);
        _discovery = new UdpLobbyDiscovery(diagnostics);
        _app = new LudoApplication(
            new TcpPeerNetwork(diagnostics),
            _discovery,
            new Sha256CryptoProvider(),
            presenter,
            TimeProvider.System,
            diagnostics);

        var window = new MainWindow { DataContext = new MainViewModel(_app, presenter, diagnostics) };
        window.ProtectionFailed += (_, _) =>
        {
            MessageBox.Show(
                "DiceDrop could not enable screen-capture protection on this PC, so it will not run.\n\n" +
                "Windows 10 or 11 with desktop composition is required.",
                "DiceDrop",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            _tray?.Exit(2);
        };

        _tray = new TrayShell(window, presenter, Shutdown);
        _chatToasts = new ChatToastController(window, presenter);
        _instance.ListenForActivation(() => Dispatcher.BeginInvoke(window.BringToFront));
        StartupRegistration.RefreshPath();
        MainWindow = window;

        // Started with Windows: stay in the tray. The lobby still forms and chat still shows as toasts.
        if (!startInTray)
        {
            window.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Tell peers we are leaving; don't hang the exit if the network is wedged.
        _app?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
        _discovery?.Dispose();
        _windowGuard?.Dispose();
        _tray?.Dispose();
        _instance?.Dispose();
        base.OnExit(e);
    }
}
