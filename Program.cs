using System;
using System.Windows.Forms;

namespace ShutdownSchedule
{
    internal static partial class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}
