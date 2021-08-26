using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DotNetCommons.Temporal;
using DotNetCommons.Text;
using DotNetCommons.WinForms;

namespace DesktopCompanion
{
    public class Wallpaper
    {
        private string _screenInfo;
        private readonly AppSettings _appSettings;
        private readonly DirectoryInfo _directory;
        private readonly List<string> _list = new();

        public string CurrentFileName => _list.Count == 0
            ? null
            : _list[((int)DateTime.Today.ToOADate() + _appSettings.WallpaperOffset) % _list.Count];

        public Wallpaper(AppSettings appSettings, DirectoryInfo directory)
        {
            _appSettings = appSettings;
            _directory = directory;
            Rescan();
        }

        private string MakeScreenInfo()
        {
            return string.Join("|", Screen.AllScreens.Length,
                SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, CurrentFileName);
        }

        public void Rescan()
        {
            _list.Clear();
            _list.AddRange(
                _directory.EnumerateFiles("*.jpg", SearchOption.TopDirectoryOnly)
                    .Concat(_directory.EnumerateFiles("*.jpeg", SearchOption.TopDirectoryOnly))
                    .Concat(_directory.EnumerateFiles("*.png", SearchOption.TopDirectoryOnly))
                    .Select(x => x.Name)
            );
        }

        public void Update()
        {
            _screenInfo = MakeScreenInfo();
            SetWallpaper(Path.Combine(_directory.FullName, CurrentFileName));
        }

        public bool UpdateIfChanged()
        {
            if (MakeScreenInfo() == _screenInfo)
                return false;

            Update();
            return true;
        }

        private void SetWallpaper(string fileName)
        {
            using var bmp = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, PixelFormat.Format24bppRgb);
            using var source = Image.FromFile(fileName);
            using var g = Graphics.FromImage(bmp);

            g.DrawImage(source, 0, 0, source.Width, source.Height);

            PaintInformation(bmp, g, source.Size);

            var wallpaperFileName = Path.Combine(Path.GetTempPath(), "DesktopCompanion.jpg");
            bmp.Save(wallpaperFileName);

            WinApi.SystemParametersInfo(WinApi.SPI.SETDESKWALLPAPER, 0, wallpaperFileName,
                WinApi.SPIF.SPIF_SENDCHANGE | WinApi.SPIF.SPIF_UPDATEINIFILE);
        }

        private void PaintInformation(Bitmap bitmap, Graphics graphics, Size size)
        {
            var x = 128;
            var y = (int)(size.Height * 0.7);
            var r = new Rectangle(x, y, Math.Min(1024, size.Width - x), Math.Min(200, size.Height - y));

            var intensity = CalculateIntensity(bitmap, r);

            using var bigFont = new Font("Archer Book", 48);
            var bigHeight = (int)Math.Round(bigFont.GetHeight(), MidpointRounding.AwayFromZero);

            using var smallFont = new Font("Archer Book", 14);
            using var smallBoldFont = new Font("Archer Book", 14, FontStyle.Bold);
            var smallHeight = (int)Math.Round(smallFont.GetHeight(), MidpointRounding.AwayFromZero);

            using var alphaBrush = new SolidBrush(intensity > 0.5 ? Color.FromArgb(96, 0, 0, 0) : Color.FromArgb(96, 255, 255, 255));
            using var solidBrush = new SolidBrush(intensity > 0.5 ? Color.FromArgb(192, 0, 0, 0) : Color.FromArgb(192, 255, 255, 255));

            graphics.DrawString(DateTime.Today.ToString("dddd d MMMM yyyy").StartUppercase(), bigFont, alphaBrush, 128, y);
            y += bigHeight;

            var holidays = _appSettings.Holidays.ToList();
            holidays.AddRange(new UnitedStatesHolidays().All);

            var upcoming = holidays.Where(h => h.DaysLeft() < 60).OrderBy(h => h.DaysLeft()).Take(5).ToArray();
            foreach (var holiday in upcoming)
            {
                var font = holiday.Name.Contains("*") ? smallBoldFont : smallFont;
                var brush = holiday.Name.Contains("*") ? solidBrush : alphaBrush;

                graphics.DrawString(holiday.NextDate.ToString("ddd d MMMM").StartUppercase(), font, brush, 140, y);
                graphics.DrawString(holiday.DaysLeft() + " d", font, brush, 310, y);
                graphics.DrawString(holiday.Name, font, brush, 360, y);
                y += smallHeight;
            }
        }

        private static unsafe double CalculateIntensity(Bitmap bitmap, Rectangle rect)
        {
            var bits = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var intensity = 0.0;
            var count = 0;
            try
            {
                var scanLine = bits.Scan0;
                for (var y = rect.Top; y < rect.Bottom; y++)
                {
                    for (var x = rect.Left; x < rect.Right; x++)
                    {
                        var ptr = (byte*)(scanLine + x * 3);
                        var r = *ptr;
                        var g = *(ptr + 1);
                        var b = *(ptr + 2);

                        intensity += Color.FromArgb(r, g, b).GetBrightness();
                        count++;
                    }

                    scanLine += bits.Stride;
                }

                intensity /= count;
            }
            finally
            {
                bitmap.UnlockBits(bits);
            }

            return intensity;
        }
    }
}
