using DotNetCommons.WinForms;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

// ReSharper disable LocalizableElement

namespace DesktopCompanion;

public class Wallpaper
{
    private static readonly Color DefaultColor = Color.FromArgb(32, 40, 48);

    public WallpaperMode[] Modes { get; init; }

    internal string ScreenInfo;
    private readonly AppSettings _appSettings;

    public Wallpaper(AppSettings appSettings)
    {
        _appSettings = appSettings;

        Modes = new WallpaperMode[]
        {
            new WallpaperSingleColorMode("Architect mode", _appSettings, DefaultColor),
            new WallpaperPictureMode(null, _appSettings, new DirectoryInfo(_appSettings.WallpaperFolder))
        };

        foreach (var mode in Modes)
            mode.Rescan();
    }

    public int CurrentModeIndex => DateTime.Now.Hour < 11 ? 0 : 1;
    public WallpaperMode CurrentMode => Modes[CurrentModeIndex];

    internal string MakeScreenInfo()
    {
        return string.Join("|",
            Screen.AllScreens.Length, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, _appSettings.Intensity,
            CurrentModeIndex, CurrentMode.State, DateTime.UtcNow.ToString("yyyyMMddHH"));
    }

    public bool UpdateIfChanged()
    {
        if (MakeScreenInfo() == ScreenInfo)
            return false;

        CurrentMode.Rescan();

        try
        {
            ScreenInfo = MakeScreenInfo();
            SetWallpaper();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void MakeWallpaper(WallpaperMode mode, FileInfo target)
    {
        using var bmp = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(bmp);

        mode.DrawImage(bmp, graphics, _appSettings.Intensity);
        DrawingHelpers.PaintInformation(bmp, graphics, bmp.Size, _appSettings, mode.Title);

        bmp.Save(target.FullName);
    }

    private void SetWallpaper()
    {
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), "DesktopCompanionColor.jpg"));

        MakeWallpaper(CurrentMode, file);

        WinApi.SystemParametersInfo(WinApi.SPI.SETDESKWALLPAPER, 0, file.FullName, WinApi.SPIF.SPIF_SENDCHANGE | WinApi.SPIF.SPIF_UPDATEINIFILE);
    }
}