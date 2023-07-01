using Eto.Drawing;
using Eto.Forms;
using System;
using Veldrid;

namespace Eto.Veldrid
{
    /// <summary>
    /// A simple control that allows drawing with Veldrid.
    /// </summary>
    [Handler(typeof(VeldridSurface.IHandler))]
    public class VeldridSurface : Control
    {
        public new interface IHandler : Control.IHandler
        {
            Size RenderSize { get; }
            Swapchain CreateSwapchain();
        }

        private new IHandler Handler => (IHandler)base.Handler;

        public new interface ICallback : Control.ICallback
        {
            void OnInitializeBackend(VeldridSurface s, InitializeEventArgs e);
            void OnDraw(VeldridSurface s, EventArgs e);
            void OnResize(VeldridSurface s, ResizeEventArgs e);
        }

        protected new class Callback : Control.Callback, ICallback
        {
            public void OnInitializeBackend(VeldridSurface s, InitializeEventArgs e) => s?.InitializeGraphicsBackend(e);
            public void OnDraw(VeldridSurface s, EventArgs e) => s?.OnDraw(e);
            public void OnResize(VeldridSurface s, ResizeEventArgs e) => s?.OnResize(e);
        }

        protected override object GetCallback() => new Callback();

        /// <summary>
        /// The render area's size, which may differ from the control's size
        /// (e.g. with high DPI displays).
        /// </summary>
        public Size RenderSize => this.Handler.RenderSize;
        /// <summary>
        /// The render area's width, which may differ from the control's width
        /// (e.g. with high DPI displays).
        /// </summary>
        public int RenderWidth => this.RenderSize.Width;
        /// <summary>
        /// The render area's height, which may differ from the control's height
        /// (e.g. with high DPI displays).
        /// </summary>
        public int RenderHeight => this.RenderSize.Height;

        public GraphicsBackend Backend { get; private set; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public GraphicsDeviceOptions GraphicsDeviceOptions { get; private set; }
        public Swapchain Swapchain { get; private set; }

        public const string VeldridInitializedEvent = "VeldridSurface.VeldridInitialized";
        public const string DrawEvent = "VeldridSurface.Draw";
        public const string ResizeEvent = "VeldridSurface.Resize";

        public event EventHandler<InitializeEventArgs> VeldridInitialized
        {
            add { this.Properties.AddHandlerEvent(VeldridInitializedEvent, value); }
            remove { this.Properties.RemoveEvent(VeldridInitializedEvent, value); }
        }
        public event EventHandler<EventArgs> Draw
        {
            add { this.Properties.AddHandlerEvent(DrawEvent, value); }
            remove { this.Properties.RemoveEvent(DrawEvent, value); }
        }
        public event EventHandler<ResizeEventArgs> Resize
        {
            add { this.Properties.AddHandlerEvent(ResizeEvent, value); }
            remove { this.Properties.RemoveEvent(ResizeEvent, value); }
        }

        public VeldridSurface(GraphicsBackend backend, GraphicsDeviceOptions gdOptions)
        {
            this.Backend = backend;
            this.GraphicsDeviceOptions = gdOptions;
        }

        private void InitializeGraphicsBackend(InitializeEventArgs e)
        {
            switch (this.Backend)
            {
                case GraphicsBackend.Metal:
                    this.GraphicsDevice = GraphicsDevice.CreateMetal(this.GraphicsDeviceOptions);
                    break;
                case GraphicsBackend.Direct3D11:
                    this.GraphicsDevice = GraphicsDevice.CreateD3D11(this.GraphicsDeviceOptions);
                    break;
                case GraphicsBackend.Vulkan:
                    this.GraphicsDevice = GraphicsDevice.CreateVulkan(this.GraphicsDeviceOptions);
                    break;
                default:
                    throw new ArgumentException("Specified backend not supported!");
            }

            this.Swapchain = this.Handler.CreateSwapchain();

            this.Properties.TriggerEvent(VeldridInitializedEvent, this, e);
        }

        protected virtual void OnDraw(EventArgs e)
        {
            this.Properties.TriggerEvent(DrawEvent, this, e);
        }

        protected virtual void OnResize(ResizeEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            this.Swapchain?.Resize((uint)e.Width, (uint)e.Height);

            this.Properties.TriggerEvent(ResizeEvent, this, e);
        }
    }
}
