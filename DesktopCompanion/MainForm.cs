using DotNetCommons;
using DotNetCommons.Text;
using DotNetCommons.WinForms;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        _wallpaper = new Wallpaper(_appSettings);
        DailyTimer.Enabled = true;
        UpdateWallpaper(false, 0, 0);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        _hotKey?.Clear();

        _hotKey = new HotKeys(Handle);
        _hotKey.Add(WinApi.MOD_WIN, (uint)Keys.Multiply, () => UpdateWallpaper(false, 1, 0));
        _hotKey.Add(WinApi.MOD_WIN, (uint)Keys.Divide, () => UpdateWallpaper(false, -1, 0));
        _hotKey.Add(WinApi.MOD_WIN | WinApi.MOD_SHIFT, (uint)Keys.Multiply, () => UpdateWallpaper(false, 0, 0.2m));
        _hotKey.Add(WinApi.MOD_WIN | WinApi.MOD_SHIFT, (uint)Keys.Divide, () => UpdateWallpaper(false, 0, -0.2m));

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
        UpdateWallpaper(false, 0, 0);
    }

    private void UpdateWallpaper(bool delay, int modifier, decimal intensityDelta)
    {
        _appSettings.WallpaperOffset += modifier;
        _appSettings.Intensity = (_appSettings.Intensity + intensityDelta).Limit(0.2m, 1.0m);
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
            UpdateWallpaper(true, 0, 0);
        else if (m.Msg == (int)WinApi.WM.POWERBROADCAST && m.WParam == (IntPtr)WinApi.PBT_APMRESUMEAUTOMATIC)
            UpdateWallpaper(true, 0, 0);
        else if (m.Msg == (int)WinApi.WM.HOTKEY)
            _hotKey.Process(ref m);

        base.WndProc(ref m);
    }
}