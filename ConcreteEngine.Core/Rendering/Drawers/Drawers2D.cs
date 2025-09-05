using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;


internal sealed class SpriteDrawer : CommandDrawer<DrawCommandSprite>
{
    public override void Draw(in DrawCommandSprite cmd)
    {
        Context.MaterialBinder.BindMaterialSlots(cmd.MaterialId);

        Gfx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
        Gfx.BindMesh(cmd.MeshId);
        Gfx.DrawMesh(cmd.DrawCount);
    }
}

internal sealed class TilemapDrawer : CommandDrawer<DrawCommandTilemap>
{
    public override void Draw(in DrawCommandTilemap cmd)
    {
        Context.MaterialBinder.BindMaterialSlots(cmd.MaterialId);

        Gfx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
        Gfx.BindMesh(cmd.MeshId);
        Gfx.DrawMesh(cmd.DrawCount);
    }
}

internal sealed class LightDrawer : CommandDrawer<DrawCommandLight>
{
    public override void Draw(in DrawCommandLight cmd)
    {
        Gfx.BindMesh(Context.Graphics.Primitives.FsqQuad);
        Gfx.SetUniform(ShaderUniform.LightPos, cmd.Position);
        Gfx.SetUniform(ShaderUniform.Radius, cmd.Radius);
        Gfx.SetUniform(ShaderUniform.Color, cmd.Color);
        Gfx.SetUniform(ShaderUniform.Intensity, cmd.Intensity);
        Gfx.SetUniform(ShaderUniform.Softness, 2.5f);
        Gfx.SetUniform(ShaderUniform.Shape, 0);
        Gfx.DrawMesh();
    }
}
