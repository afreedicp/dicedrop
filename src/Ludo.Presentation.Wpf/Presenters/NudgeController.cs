using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Ludo.Presentation.Wpf.Shell;

namespace Ludo.Presentation.Wpf.Presenters;

/// <summary>
/// What a nudge does here: a short sound and a shake of the window's content, each of which can be
/// turned off in settings. Background nudges also show as a chat pop-up, through the chat line they add.
/// </summary>
internal sealed class NudgeController
{
    private readonly Window _mainWindow;
    private readonly Func<UserSettings> _settings;

    public NudgeController(Window mainWindow, WpfSessionPresenter presenter, Func<UserSettings> settings)
    {
        _mainWindow = mainWindow;
        _settings = settings;
        presenter.NudgeReceived += _ => OnNudge();
    }

    private void OnNudge()
    {
        var settings = _settings();
        if (settings.NudgeSound)
        {
            SystemSounds.Exclamation.Play();
        }

        if (settings.NudgeShake && _mainWindow.IsVisible && _mainWindow.WindowState != WindowState.Minimized
            && _mainWindow.Content is UIElement content && SystemParameters.ClientAreaAnimation)
        {
            Shake(content);
        }
    }

    private static void Shake(UIElement content)
    {
        var shift = new TranslateTransform();
        content.RenderTransform = shift;
        var shake = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(520), FillBehavior = FillBehavior.Stop };
        double[] offsets = [-14, 12, -10, 8, -6, 4, -2, 0];
        for (var i = 0; i < offsets.Length; i++)
        {
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(offsets[i], KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(65 * (i + 1)))));
        }

        shift.BeginAnimation(TranslateTransform.XProperty, shake);
    }
}
