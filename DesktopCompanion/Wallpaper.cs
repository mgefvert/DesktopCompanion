using DotNetCommons.WinForms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DotNetCommons;
using DotNetCommons.Text;

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
        var overlayImage = GetOverlayImage();

        return string.Join("|",
            Screen.AllScreens.Length, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, _appSettings.Intensity,
            CurrentModeIndex, CurrentMode.State, DateTime.UtcNow.ToString("yyyyMMddHH"), overlayImage);
    }

    private string GetOverlayImage()
    {
        var process = Process.GetProcesses()
            .Select(x => x.ProcessName.ToLower())
            .Distinct()
            .ToHashSet();

        string overlayImage = null;
        foreach (var (overlay, image) in _appSettings.Overlays)
        {
            if (process.Contains(overlay.ToLower()))
            {
                overlayImage = image;
                break;
            }
        }

        return overlayImage;
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

        var overlay = GetOverlayImage();
        var intensity = overlay.IsSet() ? _appSettings.Intensity.Limit(0m, 0.4m) : _appSettings.Intensity;

        mode.DrawImage(bmp, graphics, intensity);
        DrawingHelpers.PaintInformation(bmp, graphics, bmp.Size, _appSettings, mode.Title);

        if (overlay.IsSet())
            DrawingHelpers.DrawOverlay(bmp, overlay);

        bmp.Save(target.FullName);
    }

    private void SetWallpaper()
    {
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), "DesktopCompanionColor.jpg"));

        MakeWallpaper(CurrentMode, file);

        WinApi.SystemParametersInfo(WinApi.SPI.SETDESKWALLPAPER, 0, file.FullName, WinApi.SPIF.SPIF_SENDCHANGE | WinApi.SPIF.SPIF_UPDATEINIFILE);
    }
}