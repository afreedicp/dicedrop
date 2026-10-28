using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ludo.Presentation.Wpf.Views.Board;

/// <summary>
/// The wizard artwork: one pose per role from the sprite sheet, recoloured to the seat's colour. Only the
/// purple robe takes the seat colour; the staff, trim, beard and glowing parts keep their own colours.
/// </summary>
internal static class WizardSprites
{
    private const int ArmSlots = 8;

    private static readonly BitmapSource[] Bases =
    [
        Load("king"),
        Load("queen"),
        Load("bishop"),
        Load("knight"),
    ];

    private static readonly BitmapSource?[,] Cache = new BitmapSource?[4, ArmSlots];

    public static BitmapSource Get(Piece piece, int arm)
    {
        var index = (int)piece;
        var slot = arm % ArmSlots;
        Cache[index, slot] ??= Recolour(Bases[index], SeatPalette.ColorOf(arm));
        return Cache[index, slot]!;
    }

    private static BitmapSource Load(string name)
    {
        var image = new BitmapImage(new Uri($"pack://application:,,,/DiceDrop;component/Assets/Wizards/{name}.png"));
        image.Freeze();
        return image;
    }

    /// <summary>Repaints the purple robe pixels in <paramref name="target"/>, keeping their light and shade.</summary>
    private static BitmapSource Recolour(BitmapSource source, Color target)
    {
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var stride = converted.PixelWidth * 4;
        var pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);

        for (var i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] == 0)
            {
                continue;
            }

            var b = pixels[i] / 255.0;
            var g = pixels[i + 1] / 255.0;
            var r = pixels[i + 2] / 255.0;
            var max = Math.Max(r, Math.Max(g, b));
            var chroma = max - Math.Min(r, Math.Min(g, b));
            if (chroma == 0)
            {
                continue;
            }

            if (!IsRobe(HueOf(r, g, b, max, chroma), chroma / max, max))
            {
                continue;
            }

            var shade = Math.Clamp(max * 1.6, 0, 1);
            pixels[i] = (byte)(target.B * shade);
            pixels[i + 1] = (byte)(target.G * shade);
            pixels[i + 2] = (byte)(target.R * shade);
        }

        var result = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }

    private static bool IsRobe(double hue, double saturation, double value) =>
        hue >= 235 && hue <= 290 && saturation > 0.2 && value < 0.75;

    private static double HueOf(double r, double g, double b, double max, double chroma)
    {
        var sector = max == r ? ((g - b) / chroma) % 6 : max == g ? ((b - r) / chroma) + 2 : ((r - g) / chroma) + 4;
        return ((sector * 60) + 360) % 360;
    }
}
