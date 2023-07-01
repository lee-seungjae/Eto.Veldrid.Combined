using Eto.Drawing;
using Eto.Forms;

namespace EtoMyApp
{
    public partial class MainForm : Form
	{
		public CheckCommand CmdAnimate { get; } = new CheckCommand
		{
			MenuText = "Animate",
			ToolTip = "Click window content to toggle animation",
			Checked = true
		};
		public CheckCommand CmdClockwise { get; } = new CheckCommand
		{
			MenuText = "&Clockwise",
			ToolTip = "Press C to toggle direction",
			Checked = true
		};

		private void InitializeComponent()
		{
            this.Title = "Veldrid in Eto";
            this.ClientSize = new Size(800, 600);

			var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
			quitCommand.Executed += (sender, e) => Application.Instance.Quit();

			var aboutCommand = new Command { MenuText = "About..." };
			aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

            this.Menu = new MenuBar
			{
				QuitItem = quitCommand,
				AboutItem = aboutCommand,
				Items =
				{
					new ButtonMenuItem { Text = "&File" },
					new ButtonMenuItem { Text = "&View", Items = { this.CmdAnimate, this.CmdClockwise } }
				}
			};
		}
	}
}
