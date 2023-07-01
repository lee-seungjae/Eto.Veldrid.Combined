using Eto.Forms;
using Eto.Veldrid;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.SPIRV;

namespace EtoMyApp
{
    public struct VertexPositionColor
    {
        public static uint SizeInBytes = (uint)Marshal.SizeOf(typeof(VertexPositionColor));

        public Vector2 Position;
        public RgbaFloat Color;

        public VertexPositionColor(Vector2 position, RgbaFloat color)
        {
            this.Position = position;
            this.Color = color;
        }
    }

    /// <summary>
    /// A class that controls rendering to a VeldridSurface.
    /// </summary>
    /// <remarks>
    /// VeldridSurface is only a basic control that lets you render to the screen
    /// using Veldrid. How exactly to do that is up to you; this driver class is
    /// only one possible approach, and in all likelihood not the most efficient.
    /// </remarks>
    public class VeldridDriver
    {
        private readonly VeldridSurface surface;

        private void Surface_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.C)
            {
                this.Clockwise = !this.Clockwise;
                e.Handled = true;
            }
        }

        private void Surface_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Buttons == MouseButtons.Primary)
            {
                this.Animate = !this.Animate;
                e.Handled = true;
            }
        }

        private void Surface_MouseWheel(object sender, MouseEventArgs e)
        {
            this.Speed += (int)e.Delta.Height;
        }

        public UITimer Clock { get; } = new UITimer();

        public CommandList CommandList { get; private set; }
        public DeviceBuffer VertexBuffer { get; private set; }
        public DeviceBuffer IndexBuffer { get; private set; }
        public Shader VertexShader { get; private set; }
        public Shader FragmentShader { get; private set; }
        public Pipeline Pipeline { get; private set; }

        public Matrix4x4 ModelMatrix { get; private set; } = Matrix4x4.Identity;
        public DeviceBuffer ModelBuffer { get; private set; }
        public ResourceSet ModelMatrixSet { get; private set; }

        public bool Animate { get; set; } = true;

        private int _direction = 1;
        public bool Clockwise
        {
            get { return this._direction == 1 ? true : false; }
            set { this._direction = value ? 1 : -1; }
        }

        public int Speed { get; set; } = 1;

        private bool Ready = false;

        public VeldridDriver(VeldridSurface surface)
        {
            this.surface = surface;

            this.surface.MouseDown += this.Surface_MouseDown;
            this.surface.KeyDown += this.Surface_KeyDown;
            this.surface.MouseWheel += this.Surface_MouseWheel;

            this.surface.Draw += (sender, e) => this.Draw();

            this.Clock.Interval = 1.0f / 60.0f;
            this.Clock.Elapsed += this.Clock_Elapsed;
        }

        private void Clock_Elapsed(object sender, EventArgs e) => this.surface.Invalidate();

        private DateTime CurrentTime;
        private DateTime PreviousTime = DateTime.Now;

        public void Draw()
        {
            if (!this.Ready)
            {
                return;
            }

            this.CommandList.Begin();

            this.CurrentTime = DateTime.Now;
            if (this.Animate)
            {
                double radians = Convert.ToDouble((this.CurrentTime - this.PreviousTime).TotalMilliseconds / 10.0);
                float degrees = Convert.ToSingle(radians * (Math.PI / 180.0));
                degrees *= this.Speed;

                this.ModelMatrix *= Matrix4x4.CreateFromAxisAngle(
                    new Vector3(0, 0, this._direction),
                    degrees);
            }
            this.PreviousTime = this.CurrentTime;
            this.CommandList.UpdateBuffer(this.ModelBuffer, 0, this.ModelMatrix);

            this.CommandList.SetFramebuffer(this.surface.Swapchain.Framebuffer);

            // These commands differ from the stock Veldrid "Getting Started"
            // tutorial in two ways. First, the viewport is cleared to pink
            // instead of black so as to more easily distinguish between errors
            // in creating a graphics context and errors drawing vertices within
            // said context. Second, this project creates its swapchain with a
            // depth buffer, and that buffer needs to be reset at the start of
            // each frame.
            this.CommandList.ClearColorTarget(0, RgbaFloat.Pink);
            this.CommandList.ClearDepthStencil(1.0f);

            this.CommandList.SetVertexBuffer(0, this.VertexBuffer);
            this.CommandList.SetIndexBuffer(this.IndexBuffer, IndexFormat.UInt16);
            this.CommandList.SetPipeline(this.Pipeline);
            this.CommandList.SetGraphicsResourceSet(0, this.ModelMatrixSet);

            this.CommandList.DrawIndexed(
                indexCount: 4,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            this.CommandList.End();

            this.surface.GraphicsDevice.SubmitCommands(this.CommandList);
            this.surface.GraphicsDevice.SwapBuffers(this.surface.Swapchain);
        }

        public void SetUpVeldrid()
        {
            this.CreateResources();

            this.Ready = true;
        }

        private void CreateResources()
        {
            // Veldrid.SPIRV is an additional library that complements Veldrid
            // by simplifying the development of cross-backend shaders, and is
            // currently the recommended approach to doing so:
            //
            //   https://veldrid.dev/articles/portable-shaders.html
            //
            // If you decide against using it, you can try out Veldrid developer
            // mellinoe's other project, ShaderGen, or drive yourself crazy by
            // writing and maintaining custom shader code for each platform.
            byte[] vertexShaderSpirvBytes = this.LoadSpirvBytes(ShaderStages.Vertex);
            byte[] fragmentShaderSpirvBytes = this.LoadSpirvBytes(ShaderStages.Fragment);

            var options = new CrossCompileOptions();
            switch (this.surface.GraphicsDevice.BackendType)
            {
                // InvertVertexOutputY and FixClipSpaceZ address two major
                // differences between Veldrid's various graphics APIs, as
                // discussed here:
                //
                //   https://veldrid.dev/articles/backend-differences.html
                //
                // Note that the only reason those options are useful in this
                // example project is that the vertices being drawn are stored
                // the way Vulkan stores vertex data. The options will therefore
                // properly convert from the Vulkan style to whatever's used by
                // the destination backend. If you store vertices in a different
                // coordinate system, these may not do anything for you, and
                // you'll need to handle the difference in your shader code.
                case GraphicsBackend.Metal:
                    options.InvertVertexOutputY = true;
                    break;
                case GraphicsBackend.Direct3D11:
                    options.InvertVertexOutputY = true;
                    break;
                case GraphicsBackend.OpenGL:
                    options.FixClipSpaceZ = true;
                    options.InvertVertexOutputY = true;
                    break;
                default:
                    break;
            }

            ResourceFactory factory = this.surface.GraphicsDevice.ResourceFactory;

            var vertex = new ShaderDescription(ShaderStages.Vertex, vertexShaderSpirvBytes, "main", true);
            var fragment = new ShaderDescription(ShaderStages.Fragment, fragmentShaderSpirvBytes, "main", true);
            Shader[] shaders = factory.CreateFromSpirv(vertex, fragment, options);

            ResourceLayout modelMatrixLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(
                        "ModelMatrix",
                        ResourceKind.UniformBuffer,
                        ShaderStages.Vertex)));

            this.ModelBuffer = factory.CreateBuffer(
                new BufferDescription(64, BufferUsage.UniformBuffer));

            this.ModelMatrixSet = factory.CreateResourceSet(new ResourceSetDescription(
                modelMatrixLayout, this.ModelBuffer));

            VertexPositionColor[] quadVertices =
            {
                new VertexPositionColor(new Vector2(-.75f, -.75f), RgbaFloat.Red),
                new VertexPositionColor(new Vector2(.75f, -.75f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-.75f, .75f), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(.75f, .75f), RgbaFloat.Yellow)
            };

            ushort[] quadIndices = { 0, 1, 2, 3 };

            this.VertexBuffer = factory.CreateBuffer(new BufferDescription(4 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
            this.IndexBuffer = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));

            this.surface.GraphicsDevice.UpdateBuffer(this.VertexBuffer, 0, quadVertices);
            this.surface.GraphicsDevice.UpdateBuffer(this.IndexBuffer, 0, quadIndices);

            // Veldrid.SPIRV, when cross-compiling to HLSL, will always produce
            // TEXCOORD semantics; VertexElementSemantic.TextureCoordinate thus
            // becomes necessary to let D3D11 work alongside Vulkan and OpenGL.
            //
            //   https://github.com/mellinoe/veldrid/issues/121
            //
            var vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

            this.Pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false),
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                ResourceLayouts = new[] { modelMatrixLayout },
                ShaderSet = new ShaderSetDescription(
                    vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                    shaders: shaders),
                Outputs = this.surface.Swapchain.Framebuffer.OutputDescription
            });

            this.CommandList = factory.CreateCommandList();
        }

        private byte[] LoadSpirvBytes(ShaderStages stage)
        {
            if (stage == ShaderStages.Fragment)
            {
                return System.Text.Encoding.UTF8.GetBytes(@"#version 450

layout (location = 0) in vec4 fsin_Color;

layout (location = 0) out vec4 fsout_Color;

void main()
{
	fsout_Color = fsin_Color;
}

");
            }
            else if (stage == ShaderStages.Vertex)
            {
                return System.Text.Encoding.UTF8.GetBytes(@"#version 450

layout (location = 0) in vec2 Position;
layout (location = 1) in vec4 Color;

layout (location = 0) out vec4 fsin_Color;

layout (set = 0, binding = 0) uniform ModelMatrix
{
	mat4 model;
};

void main()
{
	gl_Position = model * vec4(Position, 0, 1);

	fsin_Color = Color;
}

");
            }
            else
            {
                throw new NotImplementedException(stage.ToString());
            }
        }
    }
}
