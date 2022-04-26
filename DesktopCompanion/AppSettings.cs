using DotNetCommons.Temporal;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopCompanion;

public class AppSettings
{
    private const string RegistryKeyMain = "Software\\Gefvert\\DesktopCompanion";
    private const string RegistryKeyHolidays = "Software\\Gefvert\\DesktopCompanion\\Holidays";

    private readonly RegistryKey _regKeyDesktopCompanion;
    private readonly RegistryKey _regKeyDesktopCompanionHolidays;

    public AppSettings()
    {
        var hive = Registry.CurrentUser;
        _regKeyDesktopCompanion = hive.CreateSubKey(RegistryKeyMain, true);
        _regKeyDesktopCompanionHolidays = hive.CreateSubKey(RegistryKeyHolidays, false);

        if (_regKeyDesktopCompanion == null || _regKeyDesktopCompanionHolidays == null)
            throw new Exception("Unable to create registry configuration.");

        DesktopFont ??= "Bahnschrift";
        WallpaperFolder ??= Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Wallpapers");
    }

    public string DesktopFont
    {
        get => (string)_regKeyDesktopCompanion.GetValue(nameof(DesktopFont), null);
        set => _regKeyDesktopCompanion.SetValue(nameof(DesktopFont), value);
    }

    public decimal Intensity
    {
        get => (int)(_regKeyDesktopCompanion.GetValue(nameof(Intensity), 100) ?? 100) / 100m;
        set => _regKeyDesktopCompanion.SetValue(nameof(Intensity), (int)(value * 100));
    }

    public string WallpaperFolder
    {
        get => (string)_regKeyDesktopCompanion.GetValue(nameof(WallpaperFolder), null);
        set => _regKeyDesktopCompanion.SetValue(nameof(WallpaperFolder), value);
    }

    public string WallpaperFolderArchitect
    {
        get => (string)_regKeyDesktopCompanion.GetValue(nameof(WallpaperFolderArchitect), null);
        set => _regKeyDesktopCompanion.SetValue(nameof(WallpaperFolderArchitect), value);
    }

    public int WallpaperOffset
    {
        get => (int)(_regKeyDesktopCompanion.GetValue(nameof(WallpaperOffset), 0) ?? 0);
        set => _regKeyDesktopCompanion.SetValue(nameof(WallpaperOffset), value);
    }

    public IEnumerable<Holiday> Holidays =>
        _regKeyDesktopCompanionHolidays.GetValueNames()
            .Select(x => (string)_regKeyDesktopCompanionHolidays.GetValue(x))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Holiday.Create);
}