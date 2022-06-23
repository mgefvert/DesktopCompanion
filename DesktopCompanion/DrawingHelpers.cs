using DotNetCommons.Temporal;
using DotNetCommons.Text;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DesktopCompanion;

internal static class DrawingHelpers
{
    internal static unsafe double CalculateIntensity(Bitmap bitmap, Rectangle rect)
    {
        var bits = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var intensity = 0.0;
        var count = 0;
        try
        {
            var scanLine = bits.Scan0;
            for (var y = rect.Top; y < rect.Bottom - 1; y++)
            {
                for (var x = rect.Left; x < rect.Right - 1; x++)
                {
                    var ptr = (byte*)(scanLine + x * 4);
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

    internal static void PaintInformation(Bitmap bitmap, Graphics graphics, Size size, AppSettings appSettings, string title)
    {
        var x = 128;
        var y = (int)(size.Height * 0.7);
        var r = new Rectangle(x, y, Math.Min(1024, size.Width - x), Math.Min(200, size.Height - y));

        var intensity = CalculateIntensity(bitmap, r);

        using var bigFont = new Font(appSettings.DesktopFont, 48);
        var bigHeight = (int)Math.Round(bigFont.GetHeight(), MidpointRounding.AwayFromZero);

        using var mediumFont = new Font(appSettings.DesktopFont, 32);
        var mediumHeight = (int)Math.Round(mediumFont.GetHeight(), MidpointRounding.AwayFromZero);

        using var smallFont = new Font(appSettings.DesktopFont, 14);
        using var smallBoldFont = new Font(appSettings.DesktopFont, 14, FontStyle.Bold);
        var smallHeight = (int)Math.Round(smallFont.GetHeight(), MidpointRounding.AwayFromZero);

        using var alphaBrush = new SolidBrush(intensity > 0.5 ? Color.FromArgb(96, 0, 0, 0) : Color.FromArgb(96, 255, 255, 255));
        using var solidBrush = new SolidBrush(intensity > 0.5 ? Color.FromArgb(192, 0, 0, 0) : Color.FromArgb(192, 255, 255, 255));

        graphics.DrawString(DateTime.Today.ToString("dddd d MMMM yyyy").StartUppercase(), mediumFont, alphaBrush, 128, y);
        y += mediumHeight;
        graphics.DrawString(DailyMessages.GetDailyMessage(), smallFont, alphaBrush, 140, y);

        var holidays = appSettings.Holidays.ToList();
        holidays.AddRange(new UnitedStatesHolidays().All);
        y += (int)(smallHeight * 1.8);

        var upcoming = holidays.Where(h => h.DaysLeft() < 60).OrderBy(h => h.DaysLeft()).Take(5).ToArray();
        foreach (var holiday in upcoming)
        {
            var asterisk = holiday.Name?.Contains("*") ?? false;
            var font = asterisk ? smallBoldFont : smallFont;
            var brush = asterisk ? solidBrush : alphaBrush;

            graphics.DrawString(holiday.NextDate.ToString("ddd d MMMM").StartUppercase(), font, brush, 140, y);
            graphics.DrawString(holiday.DaysLeft() + " d", font, brush, 310, y);
            graphics.DrawString(holiday.Name, font, brush, 360, y);
            y += smallHeight;
        }

        y = (int)(size.Height * 0.2);
        graphics.DrawString("Week " + ISOWeek.GetWeekOfYear(DateTime.Today) + ", " + DateTime.Today.Year, bigFont, alphaBrush, 128, y);

        var otherStrings = new List<string>();
        if (!string.IsNullOrEmpty(title))
            otherStrings.Add(title);
        if (appSettings.Intensity != 1.0m)
            otherStrings.Add($"Intensity {appSettings.Intensity:P0}");

        y += bigHeight;
        graphics.DrawString(string.Join("; ", otherStrings), smallFont, alphaBrush, 140, y);
    }

    public static unsafe void SetAlpha(Bitmap bitmap, byte alpha)
    {
        var size = bitmap.Size;
        var rect = new Rectangle(0, 0, size.Width, size.Height);

        var bits = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            var ptr = (byte*)bits.Scan0 + 3;
            for (var i = 0; i < size.Width * size.Height; i++, ptr += 4)
                *ptr = alpha;
        }
        finally
        {
            bitmap.UnlockBits(bits);
        }
    }

    public static void DrawOverlay(Bitmap bitmap, string filename)
    {
        if (!File.Exists(filename))
            return;

        using var overlay = Image.FromFile(filename);
        using var graphics = Graphics.FromImage(bitmap);

        var monitorSize = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
        var position = new Point((monitorSize.Width - overlay.Width) / 2, (monitorSize.Height - overlay.Height) / 2);

        graphics.DrawImageUnscaled(overlay, position);
    }
}