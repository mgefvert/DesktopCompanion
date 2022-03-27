using System;
using System.Threading;
using System.Windows.Forms;
using DotNetCommons;

namespace DesktopCompanion;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.ThreadException += HandleThreadException;
        Application.Run(new MainForm { ShowInTaskbar = false });
    }

    private static void HandleThreadException(object sender, ThreadExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.GetDetailedInformation(false), e.Exception.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}