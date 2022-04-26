using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

// ReSharper disable LocalizableElement

namespace DesktopCompanion;

public abstract class WallpaperMode
{
    protected AppSettings AppSettings;
    public string Title { get; set; }
    public abstract string State { get; }

    protected WallpaperMode(string title, AppSettings appSettings)
    {
        Title = title;
        AppSettings = appSettings;
    }

    public abstract void DrawImage(Bitmap image, Graphics graphics, decimal intensity);
    public virtual void Rescan() { }
}

public class WallpaperSingleColorMode : WallpaperMode
{
    public Color SingleColor { get; }
    public override string State => SingleColor.ToString();

    public WallpaperSingleColorMode(string title, AppSettings appSettings, Color singleColor) : base(title, appSettings)
    {
        SingleColor = singleColor;
    }

    public override void DrawImage(Bitmap image, Graphics graphics, decimal intensity)
    {
        using var brush = new SolidBrush(SingleColor);
        graphics.FillRectangle(brush, 0, 0, image.Width, image.Height);
    }
}

public class WallpaperPictureMode : WallpaperMode
{
    public DirectoryInfo Directory { get; }
    public List<FileInfo> Files { get; } = new();
    public override string State => CurrentFile.Name;

    public FileInfo CurrentFile => Files.Count == 0
        ? null
        : Files[((int)DateTime.Today.ToOADate() + AppSettings.WallpaperOffset) % Files.Count];

    public WallpaperPictureMode(string title, AppSettings appSettings, DirectoryInfo directory) : base(title, appSettings)
    {
        Directory = directory;
    }

    public override void DrawImage(Bitmap image, Graphics graphics, decimal intensity)
    {
        // Load image
        using var source = new Bitmap(CurrentFile.FullName);

        // Convert to a 
        using var intermediate = new Bitmap(source);
        using var bmp = intermediate.Clone(new Rectangle(0, 0, source.Width, source.Height), PixelFormat.Format32bppArgb);

        DrawingHelpers.SetAlpha(bmp, (byte)(intensity * 255));

        graphics.DrawImage(bmp, 0, 0, source.Width, source.Height);
    }

    public override void Rescan()
    {
        Files.Clear();

        var files = Directory.EnumerateFiles("*.jpg", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles("*.jpeg", SearchOption.TopDirectoryOnly))
            .Concat(Directory.EnumerateFiles("*.png", SearchOption.TopDirectoryOnly))
            .OrderBy(x => x.Name);

        Files.AddRange(files);
    }
}