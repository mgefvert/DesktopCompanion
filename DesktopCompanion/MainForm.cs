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
        private DateTime _lastDate;
        private HotKeys _hotkey;

        public MainForm()
        {
            InitializeComponent();

            _appSettings = new AppSettings();
            _wallpaper = new Wallpaper(_appSettings, new DirectoryInfo(@"D:\dropbox\misc\dual"));
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

            _hotkey?.Clear();

            _hotkey = new HotKeys(Handle);
            _hotkey.Add(WinApi.MOD_WIN, (uint)Keys.Multiply, () => ChangeWallpaper(1));
            _hotkey.Add(WinApi.MOD_WIN, (uint)Keys.Divide, () => ChangeWallpaper(-1));
            if (!_hotkey.AllSucceeded())
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
            if (DateTime.Today <= _lastDate)
                return;

            _lastDate = DateTime.Today;
            _wallpaper.Update();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)WinApi.WM.DISPLAYCHANGE)
            {
                ChangeWallpaperTimer.Enabled = false;
                ChangeWallpaperTimer.Enabled = true;
            }
            else if (m.Msg == (int)WinApi.WM.HOTKEY)
                _hotkey.Process(ref m);

            base.WndProc(ref m);
        }
    }
}
