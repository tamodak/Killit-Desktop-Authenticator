using System;
using System.Windows.Forms;
using KillitDesktopAuthenticator.Adb;

namespace KillitDesktopAuthenticator
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // The UI language follows the Windows display language; ResourceManager falls back to
            // English for anything Killit does not translate.
            var provisioner = new Provisioner(new AdbClient(AdbClient.BundledPath));
            Application.Run(new MainForm(provisioner));
        }
    }
}
