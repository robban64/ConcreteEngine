namespace ConcreteEngine.Core.Specs.Graphics;

public static class Extensions
{
    extension(GraphicsHandleKind kind)
    {
        public string ToResourceName()
        {
            return kind switch
            {
                GraphicsHandleKind.Texture => "Texture",
                GraphicsHandleKind.Shader => "Shader",
                GraphicsHandleKind.Mesh => "Mesh",
                GraphicsHandleKind.VertexBuffer => "VertexBuffer",
                GraphicsHandleKind.IndexBuffer => "IndexBuffer",
                GraphicsHandleKind.UniformBuffer => "UniformBuffer",
                GraphicsHandleKind.FrameBuffer => "FrameBuffer",
                GraphicsHandleKind.RenderBuffer => "RenderBuffer",
                _ => "Invalid"
            };
        }

        public string ToShortText()
        {
            return kind switch
            {
                GraphicsHandleKind.Texture => "TEX",
                GraphicsHandleKind.Shader => "SHD",
                GraphicsHandleKind.Mesh => "MSH",
                GraphicsHandleKind.VertexBuffer => "VBO",
                GraphicsHandleKind.IndexBuffer => "IBO",
                GraphicsHandleKind.UniformBuffer => "UBO",
                GraphicsHandleKind.FrameBuffer => "FBO",
                GraphicsHandleKind.RenderBuffer => "RBO",
                _ => "INV"
            };
        }
    }
}