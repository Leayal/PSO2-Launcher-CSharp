using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Leayal.PSO2Launcher.Core.Classes
{
    static class BitmapSourceHelper
    {
        /* WritableBitmapEx's stuffs
        public unsafe static void InverseColorWithoutCreatingCopy(this WriteableBitmap bm)
        {
            bm.Lock();
            try
            {
                using (var context = bm.GetBitmapContext(ReadWriteMode.ReadWrite))
                {
                    Color color;
                    for (int y = 0; y < context.Height; y++)
                        for (int x = 0; x < context.Width; x++)
                        {
                            color = bm.GetPixel(x, y);
                            bm.SetPixel(x, y, new Color()
                            {
                                A = color.A,
                                R = color.R,
                                G = color.G,
                                B = color.B
                            });
                        }
                    bm.AddDirtyRect(new System.Windows.Int32Rect(0, 0, context.Width, context.Height));
                }
            }
            finally
            {
                bm.Unlock();
            }
        }
        */

        public static BitmapImage FromEmbedResourcePath(string path)
            => FromEmbedResourcePath(Assembly.GetExecutingAssembly(), path);

        public static BitmapImage FromEmbedResourcePath(Assembly asm, string path)
        {
            using (var stream = asm.GetManifestResourceStream(path))
            {
                if (stream != null)
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
                else
                {
                    return null;
                }
            }
        }

        public unsafe static WriteableBitmap CreateCopy(BitmapSource source)
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
