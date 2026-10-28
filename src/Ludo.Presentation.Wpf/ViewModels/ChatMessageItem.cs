using System.Windows.Media;
using Ludo.Application.Session;
using Ludo.Presentation.Wpf.Views.Board;

namespace Ludo.Presentation.Wpf.ViewModels;

/// <summary>
/// One chat line as the chat panel draws it. Lines from the same sender in a row form a group:
/// only the first shows the avatar and name.
/// </summary>
/// <param name="Time">When this PC first saw the line; chat carries no timestamps between peers.</param>
public sealed record ChatMessageItem(ChatLineView Line, bool StartsGroup, DateTime Time)
{
    public string Sender => Line.Sender;

    public string Text => Line.Text;

    public bool IsLocal => Line.IsLocal;

    public string Initials => InitialsOf(Line.Sender);

    public Brush AvatarColor => AvatarColorOf(Line.Sender);

    public string TimeLabel => Time.ToString("t");

    public static string InitialsOf(string name)
    {
        var words = name.Split([' ', '.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => "?",
            1 => words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant(),
            _ => $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[^1][0])}",
        };
    }

    /// <summary>A board colour picked from the name, so each player keeps the same colour on every PC.</summary>
    public static Brush AvatarColorOf(string name)
    {
        var hash = 2166136261u;
        foreach (var c in name)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return SeatPalette.Brush((int)(hash % 8));
    }
}
