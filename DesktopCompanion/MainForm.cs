using System;
using System.IO;
using System.Windows.Forms;
using DotNetCommons.WinForms;

// ReSharper disable LocalizableElement

namespace DesktopCompanion
{
    public partial class MainForm : Form
    {
        private readonly Wallpaper _wallpaper;
        private readonly AppSettings _appSettings;
        private HotKeys _hotKey;

        public MainForm()
        {
            InitializeComponent();

            _appSettings = new AppSettings();
            _wallpaper = new Wallpaper(_appSettings, new DirectoryInfo(_appSettings.WallpaperFolder));
            ChangeWallpaperTimer.Enabled = true;
        }

        private void ChangeWallpaper(int modifier)
        {
            _appSettings.WallpaperOffset += modifier;
            _wallpaper.UpdateIfChanged();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            _hotKey?.Clear();

            _hotKey = new HotKeys(Handle);
            _hotKey.Add(WinApi.MOD_WIN, (uint)Keys.Multiply, () => ChangeWallpaper(1));
            _hotKey.Add(WinApi.MOD_WIN, (uint)Keys.Divide, () => ChangeWallpaper(-1));
            if (!_hotKey.AllSucceeded())
                MessageBox.Show("Couldn't register hotkeys", "Desktop Companion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        private void AppMenuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ChangeWallpaperTimer_Tick(object sender, System.EventArgs e)
        {
            if (Handle == IntPtr.Zero)
                CreateHandle();

            ChangeWallpaperTimer.Enabled = false;
            _wallpaper.UpdateIfChanged();
        }

        private void DailyTimer_Tick(object sender, EventArgs e)
        {
            _wallpaper.UpdateIfChanged();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)WinApi.WM.DISPLAYCHANGE)
                ScheduleWallpaperTimer();
            else if (m.Msg == (int)WinApi.WM.POWERBROADCAST && m.WParam == (IntPtr)WinApi.PBT_APMRESUMEAUTOMATIC)
                ScheduleWallpaperTimer();
            else if (m.Msg == (int)WinApi.WM.HOTKEY)
                _hotKey.Process(ref m);

            base.WndProc(ref m);
        }

        private void ScheduleWallpaperTimer()
        {
            ChangeWallpaperTimer.Enabled = false;
            ChangeWallpaperTimer.Enabled = true;
        }
    }
}
