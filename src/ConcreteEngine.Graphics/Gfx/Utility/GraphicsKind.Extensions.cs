namespace ConcreteEngine.Graphics.Gfx.Utility;

public static class GraphicsKindExtensions
{
    extension(GraphicsKind kind)
    {
        public string ToResourceName()
        {
            return kind switch
            {
                GraphicsKind.Texture => "Texture",
                GraphicsKind.Shader => "Shader",
                GraphicsKind.Mesh => "Mesh",
                GraphicsKind.VertexBuffer => "VertexBuffer",
                GraphicsKind.IndexBuffer => "IndexBuffer",
                GraphicsKind.UniformBuffer => "UniformBuffer",
                GraphicsKind.FrameBuffer => "FrameBuffer",
                GraphicsKind.RenderBuffer => "RenderBuffer",
                _ => "Invalid"
            };
        }

        public string ToShortText()
        {
            return kind switch
            {
                GraphicsKind.Texture => "TEX",
                GraphicsKind.Shader => "SHD",
                GraphicsKind.Mesh => "MSH",
                GraphicsKind.VertexBuffer => "VBO",
                GraphicsKind.IndexBuffer => "IBO",
                GraphicsKind.UniformBuffer => "UBO",
                GraphicsKind.FrameBuffer => "FBO",
                GraphicsKind.RenderBuffer => "RBO",
                _ => "INV"
            };
        }
    }
}