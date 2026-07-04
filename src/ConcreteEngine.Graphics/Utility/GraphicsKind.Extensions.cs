namespace ConcreteEngine.Graphics.Utility;

public static class GraphicsKindExtensions
{
    extension(GraphicsKind kind)
    {
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