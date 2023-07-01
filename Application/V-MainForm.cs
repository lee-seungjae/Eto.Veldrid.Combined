﻿using Eto.Forms;
using Eto.Veldrid;
using System;
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
            get { return _veldridReady; }
            private set
            {
                _veldridReady = value;

                SetUpVeldrid();
            }
        }

        private bool _formReady = false;
        public bool FormReady
        {
            get { return _formReady; }
            set
            {
                _formReady = value;

                SetUpVeldrid();
            }
        }

        public MainForm() : this(VeldridSurface.PreferredBackend)
        {
        }
        public MainForm(GraphicsBackend backend)
        {
            InitializeComponent();

            Shown += (sender, e) => FormReady = true;

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
                PixelFormat.R32_Float,
                false,
                ResourceBindingModel.Improved);

            Surface = new VeldridSurface(backend, options);
            Surface.Size = new Eto.Drawing.Size(200, 200);
            Surface.VeldridInitialized += (sender, e) => VeldridReady = true;

            Content = Surface;

            Driver = new VeldridDriver
            {
                Surface = Surface
            };

            // TODO: Make this binding actually work both ways.
            CmdAnimate.Bind<bool>("Checked", Driver, "Animate");
            CmdClockwise.Bind<bool>("Checked", Driver, "Clockwise");
        }

        private void SetUpVeldrid()
        {
            if (!(FormReady && VeldridReady))
            {
                return;
            }

            Driver.SetUpVeldrid();

            Title = $"Veldrid backend: {Surface.Backend.ToString()}";

            Driver.Clock.Start();
        }
    }
}
