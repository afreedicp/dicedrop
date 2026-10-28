using System.Windows.Media.Imaging;

namespace Ludo.Presentation.Wpf.Views.Board;

/// <summary>The spell artwork from the sprite sheets: red fire, already recoloured.</summary>
internal static class SpellSprites
{
    public static BitmapSource Fireball { get; } = Load("fire_fireball");

    public static BitmapSource Flames { get; } = Load("fire_flames");

    private static BitmapSource Load(string name)
    {
        var image = new BitmapImage(new Uri($"pack://application:,,,/DiceDrop;component/Assets/Spells/{name}.png"));
        image.Freeze();
        return image;
    }
}
