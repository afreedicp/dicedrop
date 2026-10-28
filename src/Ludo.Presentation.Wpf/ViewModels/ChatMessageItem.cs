using Ludo.Application.Session;

namespace Ludo.Presentation.Wpf.ViewModels;

/// <summary>
/// One chat line as the chat panel draws it. Lines from the same sender in a row form a group:
/// only the first shows the avatar and name.
/// </summary>
/// <param name="Sender">The sender, shared with the people list so its avatar stays current.</param>
public sealed record ChatMessageItem(ChatLineView Line, bool StartsGroup, PersonViewModel Sender)
{
    public string Text => Line.Text;

    public bool IsLocal => Line.IsLocal;

    public bool IsNudge => Line.Kind == ChatLineKind.Nudge;

    public string TimeLabel => Line.Time.ToLocalTime().ToString("t");
}
