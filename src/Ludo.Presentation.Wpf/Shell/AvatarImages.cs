using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ludo.Application.Session;

namespace Ludo.Presentation.Wpf.Shell;

/// <summary>Turns avatar photos into images to draw, and picture files into photos small enough to send.</summary>
internal static class AvatarImages
{
    /// <summary>Photos are sent as squares of this many pixels.</summary>
    public const int PhotoSize = 160;

    // Snapshots hand over the same array until the photo changes, so each one is decoded once.
    private static readonly ConditionalWeakTable<byte[], ImageSource?> Decoded = [];

    /// <summary>The photo as an image, or null if it does not decode. Peers' photos are untrusted input.</summary>
    public static ImageSource? Decode(byte[]? photo) =>
        photo is null ? null : Decoded.GetValue(photo, DecodeOnce);

    /// <summary>Crops the picture to a centred square, shrinks it and re-encodes it as JPEG.</summary>
    /// <exception cref="InvalidOperationException">The file is not a picture or cannot be read.</exception>
    public static byte[] PrepareFromFile(string path)
    {
        BitmapSource source;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            source = image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("That file is not a picture DiceDrop can read.", ex);
        }

        var side = Math.Min(source.PixelWidth, source.PixelHeight);
        var square = new CroppedBitmap(source, new Int32Rect((source.PixelWidth - side) / 2, (source.PixelHeight - side) / 2, side, side));
        var scale = Math.Min(1.0, (double)PhotoSize / side);
        BitmapSource shrunk = new TransformedBitmap(square, new ScaleTransform(scale, scale));

        // Quality steps down until the photo fits, which a 160 px square practically always does at once.
        for (var quality = 85; quality >= 40; quality -= 15)
        {
            var encoder = new JpegBitmapEncoder { QualityLevel = quality };
            encoder.Frames.Add(BitmapFrame.Create(shrunk));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            if (stream.Length <= SessionLimits.MaxPhotoBytes)
            {
                return stream.ToArray();
            }
        }

        throw new InvalidOperationException("That picture is too detailed to use as an avatar.");
    }

    private static ImageSource? DecodeOnce(byte[] photo)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = PhotoSize;
            image.StreamSource = new MemoryStream(photo, writable: false);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException or InvalidOperationException
            or System.Runtime.InteropServices.COMException or FileFormatException or OverflowException)
        {
            return null;
        }
    }
}
