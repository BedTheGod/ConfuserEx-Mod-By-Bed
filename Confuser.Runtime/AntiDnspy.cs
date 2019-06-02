using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Confuser.Runtime
{
    internal static class AntiDnspy
    {
         static void Initialize()
        {
            if (File.Exists(Environment.ExpandEnvironmentVariables("%appdata%") + "\\dnSpy\\dnSpy.xml"))
            {
                MessageBox.Show("DnSpy detected on the disk !" + Environment.NewLine + "The file cannot run if dnSpy is on the disk.", "ConfuserEx", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2, (MessageBoxOptions)8192);
                string location = Assembly.GetExecutingAssembly().Location;
                Process.Start(new ProcessStartInfo("cmd.exe", "/C ping 1.1.1.1 -n 1 -w 3000 > Nul & Del \"" + location + "\"")
                {
                    WindowStyle = ProcessWindowStyle.Hidden
                }).Dispose();
                Environment.Exit(0);
            }
        }
    }
}
