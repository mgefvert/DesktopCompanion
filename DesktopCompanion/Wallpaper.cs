using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DotNetCommons.Temporal;
using DotNetCommons.Text;
using DotNetCommons.WinForms;

// ReSharper disable LocalizableElement

namespace DesktopCompanion;

public class Wallpaper
{
    private static readonly Color DefaultColor = Color.FromArgb(32, 48, 64);

    private readonly Dictionary<DayOfWeek, string[]> _messages = new()
    {
        [DayOfWeek.Monday] = new[]
        {
            "What a wonderful Monday, Sir!",
            "Happy Monday, Sir!",
            "Delightful Monday, Sir!"
        },
        [DayOfWeek.Tuesday] = new[]
        {
            "A Tuesday is a great day to be alive, Sir!",
            "Tuesdays are for vacuuming and cleaning, Sir!",
            "Tuesday, remarkable day, Sir!"
        },
        [DayOfWeek.Wednesday] = new[]
        {
            "Ah, Wednesdays, the joy of the week, Sir!",
            "It's a good Wednesday today, Sir!",
            "Mid-week indeed, very good, Sir!"
        },
        [DayOfWeek.Thursday] = new[]
        {
            "Thursday - the most average day of the week, Sir!",
            "A capital Thursday it is today, Sir!",
            "Thursday - maybe a final sprint toward the weekend, Sir?"
        },
        [DayOfWeek.Friday] = new[]
        {
            "Today is Friday, Sir! A most excellent day!",
            "It's Friday today, Sir! You made it, another week!",
            "The weekend is nigh, Sir! Jolly good work!"
        },
        [DayOfWeek.Saturday] = new[]
        {
            "A day of relaxation and fun, Sir!",
            "Saturday, time for shopping and fun, Sir!",
            "",
        },
        [DayOfWeek.Sunday] = new[]
        {
            "A holy day, Sir, good for soul and spirit!",
            "A restful day, I hope, Sir?",
            "Don't mind me, I'm just watching the penguins, Sir."
        }
    };

    internal string ScreenInfo;
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

    internal string MakeScreenInfo()
    {
        return string.Join("|",
            Screen.AllScreens.Length, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height,
            CurrentFileName, DateTime.UtcNow.ToString("yyyyMMddHH"));
    }

    public void Rescan()
    {
        _list.Clear();
        _list.AddRange(GetFileList());
    }

    private List<string> GetFileList()
    {
        try
        {
            return _directory.EnumerateFiles("*.jpg", SearchOption.TopDirectoryOnly)
                .Concat(_directory.EnumerateFiles("*.jpeg", SearchOption.TopDirectoryOnly))
                .Concat(_directory.EnumerateFiles("*.png", SearchOption.TopDirectoryOnly))
                .Select(x => x.Name)
                .OrderBy(x => x)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public bool UpdateIfChanged()
    {
        if (MakeScreenInfo() == ScreenInfo)
            return false;

        Rescan();

        try
        {
            ScreenInfo = MakeScreenInfo();
            SetWallpaper(DefaultColor);
            if (CurrentFileName != null)
                SetWallpaper(Path.Combine(_directory.FullName, CurrentFileName));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetWallpaper(Color color)
    {
        using var bmp = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, PixelFormat.Format24bppRgb);
        using var brush = new SolidBrush(color);
        using var g = Graphics.FromImage(bmp);

        g.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);

        PaintInformation(bmp, g, bmp.Size);

        var wallpaperFileName = Path.Combine(Path.GetTempPath(), "DesktopCompanionColor.jpg");
        bmp.Save(wallpaperFileName);

        WinApi.SystemParametersInfo(WinApi.SPI.SETDESKWALLPAPER, 0, wallpaperFileName,
            WinApi.SPIF.SPIF_SENDCHANGE | WinApi.SPIF.SPIF_UPDATEINIFILE);
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

        using var bigFont = new Font(_appSettings.DesktopFont, 48);
        var bigHeight = (int)Math.Round(bigFont.GetHeight(), MidpointRounding.AwayFromZero);

        using var mediumFont = new Font(_appSettings.DesktopFont, 32);
        var mediumHeight = (int)Math.Round(mediumFont.GetHeight(), MidpointRounding.AwayFromZero);

        using var smallFont = new Font(_appSettings.DesktopFont, 14);
        using var smallBoldFont = new Font(_appSettings.DesktopFont, 14, FontStyle.Bold);
        var smallHeight = (int)Math.Round(smallFont.GetHeight(), MidpointRounding.AwayFromZero);

        using var alphaBrush = new SolidBrush(intensity > 0.5 ? Color.FromArgb(96, 0, 0, 0) : Color.FromArgb(96, 255, 255, 255));
        using var solidBrush = new SolidBrush(intensity > 0.5 ? Color.FromArgb(192, 0, 0, 0) : Color.FromArgb(192, 255, 255, 255));

        graphics.DrawString(DateTime.Today.ToString("dddd d MMMM yyyy").StartUppercase(), mediumFont, alphaBrush, 128, y);
        y += mediumHeight;
        graphics.DrawString(GetDailySaying(), smallFont, alphaBrush, 140, y);

        var holidays = _appSettings.Holidays.ToList();
        holidays.AddRange(new UnitedStatesHolidays().All);
        y += (int)(smallHeight * 1.8);

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

        y = (int)(size.Height * 0.2);
        graphics.DrawString("Week " + ISOWeek.GetWeekOfYear(DateTime.Today) + ", " + DateTime.Today.Year, bigFont, alphaBrush, 128, y);
    }

    private string GetDailySaying()
    {
        var sayings = _messages[DateTime.Today.DayOfWeek];
        var weekNo = (int)DateTime.Today.ToOADate() / 7;

        return sayings[weekNo % sayings.Length];
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