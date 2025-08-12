namespace ConcreteEngine.Core.Rendering;
/*
public sealed class SpriteDrawCommand: IDrawCommand
{
    public required IMesh Mesh { get; set; }
    public required IShader Shader { get; set; }
    public required ITexture2D Texture { get; set; }
    public required Matrix4X4<float> Transform { get; set; }

    public Vector2D<float> TextureOffset { get; set; } = Vector2D<float>.Zero;
    public Vector2D<float> TextureScale { get; set; } = Vector2D<float>.Zero;

    public void Execute(IGraphicsContext graphicsContext, in Matrix4X4<float> projViewMatrix)
    {
        int textureOffsetLocation = Shader.Uniforms.GetValueOrDefault("uTexOffset", -1);
        int textureScaleLocation = Shader.Uniforms.GetValueOrDefault("uTexScale", -1);

        graphicsContext.UseShader(Shader);

        //Shader.Bind();
        Shader.SetProjectionView(projViewMatrix);
        Shader.SetTransform(Transform);
        Shader.SetTextureIndex();
        Shader.SetUniform(textureOffsetLocation, TextureOffset);
        Shader.SetUniform(textureScaleLocation, TextureScale);

        graphicsContext.BindTexture(0, Texture);
        //Texture.BindDraw(0);

        graphicsContext.BindMesh(Mesh);
        graphicsContext.DrawIndexed(Mesh.DrawCount);
        //Mesh.BindDraw();
        //Mesh.Draw();
    }
}
*/