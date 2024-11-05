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
            drawable.Size = new Size(500, 500);
            this.layout.Add(drawable, Point.Empty);

            var textArea = new TextArea();
            textArea.Size = new Size(180, 20);
            this.layout.Add(textArea, new Point(10, 110));

            var timer = new UITimer();
            timer.Interval = 0.01;
            timer.Elapsed += (_, _) => drawable.Invalidate();
            timer.Start();

            Font font = new Font("맑은 고딕", 12f + (float)(DateTime.Now.Millisecond / 500.0f * Math.PI * 0.005));

            var bitmaps = new Bitmap[10000];
            for (int i = 0; i < bitmaps.Length; ++i)
            {
                Bitmap bitmap = new Bitmap(100, 30, Eto.Drawing.PixelFormat.Format32bppRgba);
                using (var graphics = new Graphics(bitmap))
                {
                    graphics.Clear(Color.FromArgb(0, 0, 0));
                    graphics.DrawText(font, Color.FromArgb(255, 0, 0), new PointF(0, 0), $"텍스트{i}");
                }
                bitmaps[i] = bitmap;
            }

            var rng = new Random();

            drawable.Paint += (_, eventArgs) =>
            {
                var sw = Stopwatch.StartNew();

                /*
                var t = (DateTime.Now.Millisecond / 1000.0f * Math.PI * 2);
                eventArgs.Graphics.DrawLine(Colors.Red, new PointF(50, 50), new PointF(50 + 50 * (float)Math.Cos(t), 50 + 50 * (float)Math.Sin(t)));
                Debug.WriteLine("draw");
                */

                for (int i = bitmaps.Length - 1; i >= 0; --i)
                {
                    eventArgs.Graphics.DrawImage(bitmaps[i], new PointF(rng.Next(500), rng.Next(400)));
                    //eventArgs.Graphics.DrawText(font, Color.FromArgb(255, 0, 0), new PointF(rng.Next(500), rng.Next(400)), $"텍스트{i}");
                }

                eventArgs.Graphics.DrawText(font, Color.FromArgb(0, 0, 0), new PointF(0, 480), $"{(int)sw.Elapsed.TotalMilliseconds}ms");
            };

            // TODO: Make this binding actually work both ways.
            this.CmdAnimate.Bind<bool>("Checked", this.Driver, "Animate");
            this.CmdClockwise.Bind<bool>("Checked", this.Driver, "Clockwise");
        }
        /*
        public int LoadTextureFromBitmap(Bitmap bitmap)
        {
            Texture2D

            int textureId;
            GL.GenTextures(1, out textureId);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                              ImageLockMode.ReadOnly,
                                              System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          bitmap.Width, bitmap.Height, 0,
            OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                          PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);

            // Set texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return textureId;
        }*/

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
