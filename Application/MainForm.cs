using Eto.Drawing;
using Eto.Forms;
using Eto.Veldrid;
using System;
using System.Diagnostics;
using Veldrid;

namespace EtoMyApp
{
    public partial class MainForm : Form
    {
        private VeldridSurface Surface;
        private VeldridDriver Driver;
        private readonly PixelLayout layout;

        public MainForm()
        {
            this.InitializeComponent();

            this.layout = new PixelLayout();
            this.Content = this.layout;

            this.SetUpVeldrid();

            var drawable = new Drawable();
            drawable.Size = new Size(100, 100);
            this.layout.Add(drawable, Point.Empty);

            var textArea = new TextArea();
            textArea.Size = new Size(180, 20);
            this.layout.Add(textArea, new Point(10, 10));

            var timer = new UITimer();
            timer.Interval = 0.01;
            timer.Elapsed += (_, _) => drawable.Invalidate();
            timer.Start();
            drawable.Paint += (_, eventArgs) => {
                var t = (DateTime.Now.Millisecond / 1000.0f * Math.PI * 2);
                eventArgs.Graphics.DrawLine(Colors.Red, new PointF(50, 50), new PointF(50 + 50 * (float)Math.Cos(t), 50 + 50 * (float)Math.Sin(t)));
                Debug.WriteLine("draw");
            };

            // TODO: Make this binding actually work both ways.
            this.CmdAnimate.Bind<bool>("Checked", this.Driver, "Animate");
            this.CmdClockwise.Bind<bool>("Checked", this.Driver, "Clockwise");
        }

        private void SetUpVeldrid()
        {
            // 플랫폼에 따라서 초기화 시점에 오류가 발생할 경우,
            // 처음으로 this.Shown 이벤트가 왔을 때 초기화하면 된다.
            // (원래 샘플이 그랬음)
            // this.Shown += (sender, e) => SetUpVeldrid()

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

            this.Surface = new VeldridSurface(GraphicsBackend.Direct3D11, options);
            this.Surface.Size = new Size(200, 200);
            this.Surface.VeldridInitialized += (sender, e) =>
            {
                this.Driver = new VeldridDriver(this.Surface);
                this.Driver.Clock.Start();
            };
            this.layout.Add(this.Surface, new Point(100, 0));

            this.Title = $"Veldrid backend: {this.Surface.Backend}";
        }
    }
}
