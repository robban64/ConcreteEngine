#region

using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

public interface ICommandRenderer;

public interface ICommandRenderer<T> : ICommandRenderer where T : struct, IDrawCommand
{
    public void Handle(in T cmd);
}

public abstract class CommandRenderer<T> : ICommandRenderer<T> where T : struct, IDrawCommand
{
    protected readonly IGraphicsDevice Graphics;
    protected readonly IGraphicsContext Gfx;
    protected readonly MaterialStore MaterialStore;

    protected MaterialId PreviousMaterial = default;

    protected CommandRenderer(IGraphicsDevice graphics, MaterialStore materialStore)
    {
        Graphics = graphics;
        Gfx = graphics.Gfx;
        MaterialStore = materialStore;
    }

    protected void BindMaterialSlots(Material material)
    {
        Gfx.UseShader(material.ShaderId);
        for (int t = 0; t < material.SamplerSlots.Length; t++)
        {
            Gfx.BindTexture(material.SamplerSlots[t], (uint)t);
        }

        var properties = material.GetProperties();
        foreach (var property in properties)
        {
            var uniform = property.Uniform;
            var kind = property.Kind;

            if (!material.TryGetValue(uniform, out var mv)) continue;

            switch (kind)
            {
                case UniformValueKind.Float:
                    if (mv is MaterialValue<float> f) Gfx.SetUniform(uniform, f.Value);
                    break;
                case UniformValueKind.Int:
                    if (mv is MaterialValue<int> i) Gfx.SetUniform(uniform, i.Value);
                    break;
                case UniformValueKind.Vec2:
                    if (mv is MaterialValue<Vector2> v2) Gfx.SetUniform(uniform, v2.Value);
                    break;
                case UniformValueKind.Vec3:
                    if (mv is MaterialValue<Vector3> v3) Gfx.SetUniform(uniform, v3.Value);
                    break;
                case UniformValueKind.Vec4:
                    if (mv is MaterialValue<Vector4> v4) Gfx.SetUniform(uniform, v4.Value);
                    break;
            }
        }
    }

    public abstract void Handle(in T cmd);
}

public sealed class SpriteRenderer(IGraphicsDevice graphics,  MaterialStore materialStore)
    : CommandRenderer<DrawCommandSprite>(graphics,  materialStore)
{
    public override void Handle(in DrawCommandSprite cmd)
    {
        var material = MaterialStore[cmd.MaterialId];
        BindMaterialSlots(material);

        Gfx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
        Gfx.BindMesh(cmd.MeshId);
        Gfx.DrawMesh(cmd.DrawCount);
    }
}

public sealed class LightRenderer(IGraphicsDevice graphics, MaterialStore materialStore)
    : CommandRenderer<DrawCommandLight>(graphics, materialStore)
{
    public override void Handle(in DrawCommandLight cmd)
    {
        Gfx.BindMesh(Graphics.Primitives.FsqQuad);
        Gfx.SetUniform(ShaderUniform.LightPos, cmd.Position);
        Gfx.SetUniform(ShaderUniform.Radius, cmd.Radius);
        Gfx.SetUniform(ShaderUniform.Color, cmd.Color);
        Gfx.SetUniform(ShaderUniform.Intensity, cmd.Intensity);
        Gfx.SetUniform(ShaderUniform.Softness, 2.5f);
        Gfx.SetUniform(ShaderUniform.Shape, 0);
        Gfx.DrawMesh();
    }
}

public sealed class MeshRenderer(IGraphicsDevice graphics,  MaterialStore materialStore)
    : CommandRenderer<DrawCommandMesh>(graphics,  materialStore)
{
    public override void Handle(in DrawCommandMesh cmd)
    {
        var material = MaterialStore[cmd.MaterialId];
        BindMaterialSlots(material);

        TransformHelper.GetNormalMatrix(in cmd.Transform, out var normalMatrix);
        Gfx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
        Gfx.SetUniform(ShaderUniform.NormalMatrix, in normalMatrix);

        Gfx.BindMesh(cmd.MeshId);
        Gfx.DrawMesh(cmd.DrawCount);
    }
}

public sealed class TerrainRenderer(IGraphicsDevice graphics,  MaterialStore materialStore)
    : CommandRenderer<DrawCommandTerrain>(graphics,  materialStore)
{
    public override void Handle(in DrawCommandTerrain cmd)
    {
        var material = MaterialStore[cmd.MaterialId];
        BindMaterialSlots(material);

        TransformHelper.GetNormalMatrix(in cmd.Transform, out var normalMatrix);
        Gfx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
        Gfx.SetUniform(ShaderUniform.NormalMatrix, in normalMatrix);
        Gfx.SetUniform(ShaderUniform.TexCoordRepeat, 20f);
        
        Gfx.BindMesh(cmd.MeshId);
        Gfx.DrawMesh(cmd.DrawCount);
    }
}

public sealed class SkyboxRenderer(IGraphicsDevice graphics,  MaterialStore materialStore)
    : CommandRenderer<DrawCommandSkybox>(graphics,  materialStore)
{
    public Camera3D Camera { get; set; }
    
    public override void Handle(in DrawCommandSkybox cmd)
    {
        Gfx.UseShader(cmd.ShaderId);
        Gfx.SetUniform(ShaderUniform.ProjectionMatrix, Camera.ProjectionMatrix);
        Gfx.SetUniform(ShaderUniform.ViewMatrix, TransformHelper.RemoveTranslation(Camera.ViewMatrix));
        Gfx.BindTexture(cmd.TextureId,0);

        var mesh = Graphics.Primitives.SkyboxCube;
        Gfx.BindMesh(mesh);
        Gfx.DrawMesh(cmd.DrawCount);
    }
}