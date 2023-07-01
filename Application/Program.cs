using Eto.Drawing;
using Eto.Forms;
using System;

namespace EtoMyApp
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            new Application(Eto.Platform.Detect).Run(new TestEtoVeldrid.MainForm());
        }
    }
}
