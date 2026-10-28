using System.Windows.Media.Imaging;

namespace Ludo.Presentation.Wpf.Views.Board;

/// <summary>The spell artwork from the sprite sheets: fire frames that cycle as they burn.</summary>
internal static class SpellSprites
{
    public static BitmapSource[] Flames { get; } = Load("fire_flame", 5);

    public static BitmapSource[] Fireballs { get; } = Load("fire_swirl", 4);

    private static BitmapSource[] Load(string name, int count) =>
        Enumerable.Range(0, count).Select(i => Load($"{name}_{i}")).ToArray();

    private static BitmapSource Load(string name)
    {
        var image = new BitmapImage(new Uri($"pack://application:,,,/DiceDrop;component/Assets/Spells/{name}.png"));
        image.Freeze();
        return image;
    }
}
