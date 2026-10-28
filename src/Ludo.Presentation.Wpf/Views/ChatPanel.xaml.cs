using System.Windows;
using System.Windows.Controls;
using Ludo.Presentation.Wpf.ViewModels;

namespace Ludo.Presentation.Wpf.Views;

public partial class ChatPanel : UserControl
{
    /// <summary>The character count appears once a message gets this close to the limit.</summary>
    private const int CounterThreshold = 100;

    public ChatPanel()
    {
        InitializeComponent();

        // Opening another conversation starts at its newest line, ready to type.
        DataContextChanged += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            Scroller.ScrollToEnd();
            NewMessages.Visibility = Visibility.Collapsed;
            if (FocusOnOpen && IsVisible && Composer.IsEnabled)
            {
                Composer.Focus();
            }
        });
    }

    /// <summary>Puts the cursor in the message box whenever a conversation opens. Off where keys mean something else, like the board.</summary>
    public bool FocusOnOpen { get; set; }

    /// <summary>
    /// Follows new lines while the newest one is in view. Scrolled up to read older ones, it stays put
    /// and offers a "New messages" button instead.
    /// </summary>
    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var atBottom = e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 1;
        if (e.ExtentHeightChange > 0)
        {
            var wasAtBottom = e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - e.ExtentHeightChange - 1;
            if (wasAtBottom)
            {
                Scroller.ScrollToEnd();
                return;
            }

            NewMessages.Visibility = Visibility.Visible;
        }

        if (atBottom)
        {
            NewMessages.Visibility = Visibility.Collapsed;
        }
    }

    private void OnNewMessagesClick(object sender, RoutedEventArgs e)
    {
        Scroller.ScrollToEnd();
        NewMessages.Visibility = Visibility.Collapsed;
        Composer.Focus();
    }

    private void OnComposerTextChanged(object sender, TextChangedEventArgs e)
    {
        var left = Composer.MaxLength - Composer.Text.Length;
        Counter.Text = left.ToString();
        Counter.Visibility = Composer.MaxLength > 0 && left <= CounterThreshold ? Visibility.Visible : Visibility.Collapsed;
    }
}

/// <summary>Our own chat lines, everyone else's, and nudges are drawn differently.</summary>
public sealed class ChatTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LocalTemplate { get; set; }

    public DataTemplate? OtherTemplate { get; set; }

    public DataTemplate? NudgeTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) => item switch
    {
        ChatMessageItem { IsNudge: true } => NudgeTemplate,
        ChatMessageItem { IsLocal: true } => LocalTemplate,
        _ => OtherTemplate,
    };
}
