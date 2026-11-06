using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace C__project_Auto_Care.Theme
{
    /// <summary>
    /// Utility class for extracting and caching the high-resolution application icon.
    /// </summary>
    public static class AppIcon
    {
        private static ImageSource _cachedIcon;

        public static ImageSource FromIco()
        {
            if (_cachedIcon != null)
            {
                return _cachedIcon;
            }

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? "";
                string exeDir = "";
                string loc = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(loc))
                {
                    exeDir = Path.GetDirectoryName(loc) ?? "";
                }

                // 1. Direct load of master 512x512 crisp PNG if present
                string[] pngCandidates = new[]
                {
                    Path.Combine(baseDir, "app_logo.png"),
                    Path.Combine(baseDir, "Resources", "app_logo.png"),
                    Path.Combine(exeDir, "app_logo.png"),
                    Path.Combine(exeDir, "Resources", "app_logo.png")
                };

                foreach (var p in pngCandidates)
                {
                    if (!string.IsNullOrEmpty(p) && File.Exists(p))
                    {
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.UriSource = new Uri(p, UriKind.Absolute);
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.EndInit();
                        bi.Freeze();
                        _cachedIcon = bi;
                        return _cachedIcon;
                    }
                }

                // 2. Load from app.ico using IconBitmapDecoder to extract the MAXIMUM resolution frame (256x256)
                string[] icoCandidates = new[]
                {
                    Path.Combine(baseDir, "app.ico"),
                    Path.Combine(exeDir, "app.ico")
                };

                foreach (var p in icoCandidates)
                {
                    if (!string.IsNullOrEmpty(p) && File.Exists(p))
                    {
                        using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            var decoder = new IconBitmapDecoder(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                            BitmapFrame bestFrame = null;
                            int maxWidth = 0;
                            foreach (var frame in decoder.Frames)
                            {
                                if (frame.PixelWidth > maxWidth)
                                {
                                    maxWidth = frame.PixelWidth;
                                    bestFrame = frame;
                                }
                            }

                            if (bestFrame != null)
                            {
                                bestFrame.Freeze();
                                _cachedIcon = bestFrame;
                                return _cachedIcon;
                            }
                        }
                    }
                }

                // 3. Fallback to resource stream
                var resImg = LoadResourceImage("app_logo", 128);
                if (resImg != null)
                {
                    _cachedIcon = resImg;
                    return _cachedIcon;
                }

                // 4. Fallback to extracting Win32 associated icon
                if (!string.IsNullOrEmpty(loc) && File.Exists(loc))
                {
                    using (var icon = Icon.ExtractAssociatedIcon(loc))
                    {
                        if (icon != null)
                        {
                            using (var ms = new MemoryStream())
                            {
                                icon.Save(ms);
                                ms.Position = 0;

                                var bi = new BitmapImage();
                                bi.BeginInit();
                                bi.StreamSource = ms;
                                bi.CacheOption = BitmapCacheOption.OnLoad;
                                bi.EndInit();
                                bi.Freeze();

                                _cachedIcon = bi;
                                return _cachedIcon;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore and fall through to null
            }

            return null;
        }

        private static BitmapImage LoadResourceImage(string resourceName, int size = 128)
        {
            try
            {
                var rm = new System.Resources.ResourceManager("C__project_Auto_Care.Properties.Resources", typeof(AppIcon).Assembly);
                var bmp = rm.GetObject(resourceName) as System.Drawing.Bitmap;
                if (bmp == null) return null;
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = ms;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.DecodePixelWidth = size;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
            catch { return null; }
        }
    }
}
