using Eto.Forms;
using System;

namespace EtoMyApp
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var app = new Application(Eto.Platform.Detect);
            app.Run(new MainForm());
        }
    }
}
