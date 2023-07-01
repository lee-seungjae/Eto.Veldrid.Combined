using Eto.Drawing;
using Eto.Forms;
using Eto.Veldrid;
using System.Diagnostics;
using Veldrid;

namespace TestEtoVeldrid
{
    public partial class MainForm : Form
    {
        private VeldridSurface Surface;
        private VeldridDriver Driver;

        private bool _veldridReady = false;
        public bool VeldridReady
        {
            get { return this._veldridReady; }
            private set
            {
                this._veldridReady = value;

                this.SetUpVeldrid();
            }
        }

        private bool _formReady = false;
        public bool FormReady
        {
            get { return this._formReady; }
            set
            {
                this._formReady = value;

                this.SetUpVeldrid();
            }
        }

        public MainForm() : this(VeldridSurface.PreferredBackend)
        {
        }
        public MainForm(GraphicsBackend backend)
        {
            this.InitializeComponent();

            var layout = new PixelLayout();
            this.Content = layout;

            Shown += (sender, e) => this.FormReady = true;

            // A depth buffer isn't strictly necessary for this project, which uses
            // only 2D vertex coordinates, but it's helpful to create one for the
            // sake of demonstration.
            //
            // The "improved" resource binding model changes how resource slots are
            // assigned in the Metal backend, allowing it to work like the others,
            // so the numbers used in calls to CommandList.SetGraphicsResourceSet
            // will make more sense to developers used to e.g. OpenGL or Direct3D.
            var options = new GraphicsDeviceOptions(
                false,
                Veldrid.PixelFormat.R32_Float,
                false,
                ResourceBindingModel.Improved);

            this.Surface = new VeldridSurface(backend, options);
            this.Surface.Size = new Eto.Drawing.Size(200, 200);
            this.Surface.VeldridInitialized += (sender, e) => this.VeldridReady = true;

            var drawable = new Drawable();
            drawable.Size = new Size(100, 100);
            layout.Add(drawable, Point.Empty);
            layout.Add(this.Surface, new Point(100, 0));

            var textArea = new TextArea();
            textArea.Size = new Size(80, 20);
            layout.Add(textArea, new Point(10, 10));


            drawable.Paint += (_, eventArgs) => {
                eventArgs.Graphics.DrawLine(Colors.Red, new PointF(0, 0), new PointF(100, 100));
                Debug.WriteLine("draw");
            };

            this.Driver = new VeldridDriver
            {
                Surface = Surface
            };

            // TODO: Make this binding actually work both ways.
            this.CmdAnimate.Bind<bool>("Checked", this.Driver, "Animate");
            this.CmdClockwise.Bind<bool>("Checked", this.Driver, "Clockwise");
        }

        private void SetUpVeldrid()
        {
            if (!(this.FormReady && this.VeldridReady))
            {
                return;
            }

            this.Driver.SetUpVeldrid();

            this.Title = $"Veldrid backend: {this.Surface.Backend.ToString()}";

            this.Driver.Clock.Start();
        }
    }
}
