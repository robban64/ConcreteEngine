using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

internal sealed class MeshDrawer: CommandDrawer<DrawCommandMesh>
{
    public override void Draw(in DrawCommandMesh cmd)
    {
        Context.MaterialBinder.BindMaterialSlots(cmd.MaterialId);

        TransformHelper.GetNormalMatrix(in cmd.Transform, out var normalMatrix);
        Gfx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
        Gfx.SetUniform(ShaderUniform.NormalMatrix, in normalMatrix);

        Gfx.BindMesh(cmd.MeshId);
        Gfx.DrawMesh(cmd.DrawCount);
    }
}

internal sealed class TerrainDrawer : CommandDrawer<DrawCommandTerrain>
{
    public override void Draw(in DrawCommandTerrain cmd)
    {
        Context.MaterialBinder.BindMaterialSlots(cmd.MaterialId);

        TransformHelper.GetNormalMatrix(in cmd.Transform, out var normalMatrix);
        Gfx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
        Gfx.SetUniform(ShaderUniform.NormalMatrix, in normalMatrix);
        Gfx.SetUniform(ShaderUniform.TexCoordRepeat, 20f);
        
        Gfx.BindMesh(cmd.MeshId);
        Gfx.DrawMesh(cmd.DrawCount);
    }
}

internal sealed class SkyboxDrawer: CommandDrawer<DrawCommandSkybox>
{
    public override void Draw(in DrawCommandSkybox cmd)
    {
        var camera = Context.Render3D.Camera;
        Gfx.UseShader(cmd.ShaderId);
        Gfx.SetUniform(ShaderUniform.ProjectionMatrix, camera.ProjectionMatrix);
        Gfx.SetUniform(ShaderUniform.ViewMatrix, TransformHelper.RemoveTranslation(camera.ViewMatrix));
        Gfx.BindTexture(cmd.TextureId,0);

        var mesh = Context.Graphics.Primitives.SkyboxCube;
        Gfx.BindMesh(mesh);
        Gfx.DrawMesh(cmd.DrawCount);
    }
}