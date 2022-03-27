using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotNetCommons.Text;
using DotNetCommons.WinForms;

// ReSharper disable LocalizableElement

namespace DesktopCompanion;

public partial class MainForm : Form
{
    private readonly SemaphoreSlim _lock = new(1);
    private readonly Wallpaper _wallpaper;
    private readonly AppSettings _appSettings;
    private HotKeys _hotKey;

    public MainForm()
    {
        InitializeComponent();

        _appSettings = new AppSettings();
        _wallpaper = new Wallpaper(_appSettings, new DirectoryInfo(_appSettings.WallpaperFolder));
        DailyTimer.Enabled = true;
        UpdateWallpaper(false);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        _hotKey?.Clear();

        _hotKey = new HotKeys(Handle);
        _hotKey.Add(WinApi.MOD_WIN, (uint)Keys.Multiply, () => UpdateWallpaper(false, 1));
        _hotKey.Add(WinApi.MOD_WIN, (uint)Keys.Divide, () => UpdateWallpaper(false, -1));

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

    private void DailyTimer_Tick(object sender, EventArgs e)
    {
        notifyIcon1.Text = (_wallpaper.ScreenInfo + Environment.NewLine + _wallpaper.MakeScreenInfo()).Left(127);
        UpdateWallpaper(false);
    }

    private void UpdateWallpaper(bool delay, int modifier = 0)
    {
        _appSettings.WallpaperOffset += modifier;
        if (Handle == IntPtr.Zero)
            CreateHandle();

        Task.Run(async () =>
        {
            if (!await _lock.WaitAsync(0))
                return;

            try
            {
                if (delay)
                    await Task.Delay(500).ConfigureAwait(true);

                _wallpaper.UpdateIfChanged();
            }
            finally
            {
                _lock.Release();
            }
        });
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == (int)WinApi.WM.DISPLAYCHANGE)
            UpdateWallpaper(true);
        else if (m.Msg == (int)WinApi.WM.POWERBROADCAST && m.WParam == (IntPtr)WinApi.PBT_APMRESUMEAUTOMATIC)
            UpdateWallpaper(true);
        else if (m.Msg == (int)WinApi.WM.HOTKEY)
            _hotKey.Process(ref m);

        base.WndProc(ref m);
    }
}