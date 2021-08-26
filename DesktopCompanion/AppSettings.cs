using System.Collections.Generic;
using System.Linq;
using DotNetCommons.Temporal;
using Microsoft.Win32;

namespace DesktopCompanion
{
    public class AppSettings
    {
        private readonly RegistryKey _regKeyDesktopCompanion;
        private readonly RegistryKey _regKeyDesktopCompanionHolidays;

        public AppSettings()
        {
            _regKeyDesktopCompanion = Registry.CurrentUser.OpenSubKey("Software\\Gefvert\\DesktopCompanion", true);
            _regKeyDesktopCompanionHolidays = Registry.CurrentUser.OpenSubKey("Software\\Gefvert\\DesktopCompanion\\Holidays", false);
        }

        public int WallpaperOffset
        {
            get => (int)(_regKeyDesktopCompanion.GetValue("WallpaperOffset", 0) ?? 0);
            set => _regKeyDesktopCompanion.SetValue("WallpaperOffset", value);
        }

        public IEnumerable<Holiday> Holidays =>
            _regKeyDesktopCompanionHolidays.GetValueNames()
                .Select(x => Holiday.Create((string)_regKeyDesktopCompanionHolidays.GetValue(x)));
    }
}
