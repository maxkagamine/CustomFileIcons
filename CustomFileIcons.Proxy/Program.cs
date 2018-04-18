using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomFileIcons.Proxy
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Handle proxying file associations for sake of having a single "default app".
            // This needs to be a separate exe as the console app would flash a console when opening a file.

            if (args.Length > 0)
            {
                Process.Start(new ProcessStartInfo(args[0], QuoteArguments.Quote(args.Skip(1))) { UseShellExecute = false });
            }
        }
    }
}
