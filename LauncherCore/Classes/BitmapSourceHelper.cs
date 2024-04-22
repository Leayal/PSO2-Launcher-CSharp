using System.Windows.Media.Imaging;
using System.Reflection;
using System.Windows.Interop;
using System.Windows;

namespace Leayal.PSO2Launcher.Core.Classes
{
    static class BitmapSourceHelper
    {
        public static BitmapSource FromWin32Icon(System.Drawing.Icon ico)
            => Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        public static BitmapImage FromEmbedResourcePath(string path)
            => FromEmbedResourcePath(Assembly.GetExecutingAssembly(), path);

        public static BitmapImage FromEmbedResourcePath(Assembly asm, string path)
        {
            using (var stream = asm.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    throw new ResourceReferenceKeyNotFoundException();
                }
                else
                {
                    var bm = new BitmapImage();
                    bm.BeginInit();
                    bm.CreateOptions = BitmapCreateOptions.None;
                    bm.CacheOption = BitmapCacheOption.OnLoad;
                    bm.StreamSource = stream;
                    bm.EndInit();
                    bm.Freeze();
                    return bm;
                }
            }
        }

        public unsafe static WriteableBitmap CreateWritableBitmapFrom(BitmapSource source)
        {
            var bm = new WriteableBitmap(source);
            bm.Lock();
            try
            {
                var rect = new System.Windows.Int32Rect(0, 0, bm.PixelWidth, bm.PixelHeight);
                source.CopyPixels(rect, bm.BackBuffer, bm.BackBufferStride * bm.PixelHeight, bm.BackBufferStride);
                bm.AddDirtyRect(rect);
            }
            finally
            {
                bm.Unlock();
            }
            return bm;
        }
    }
}
