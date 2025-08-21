using System.Numerics;
using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

public sealed class CommandRenderer
{
    private readonly ViewTransform2D _view;
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly MaterialStore _materialStore;

    private MaterialId _previousMaterial = default;

    public CommandRenderer(IGraphicsDevice graphics, ViewTransform2D view, MaterialStore materialStore)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _view = view;
        _materialStore = materialStore;
    }

    private void BindMaterialSlots(Material material)
    {
        _gfx.UseShader(material.ShaderId);
        for (int t = 0; t < material.SamplerSlots.Length; t++)
        {
            _gfx.BindTexture(material.SamplerSlots[t], (uint)t);
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
                    if (mv is MaterialValue<float> f) _gfx.SetUniform(uniform, f.Value);
                    break;
                case UniformValueKind.Int:
                    if (mv is MaterialValue<int> i) _gfx.SetUniform(uniform, i.Value);
                    break;
                case UniformValueKind.Vec2:
                    if (mv is MaterialValue<Vector2> v2) _gfx.SetUniform(uniform, v2.Value);
                    break;
                case UniformValueKind.Vec3:
                    if (mv is MaterialValue<Vector3> v3) _gfx.SetUniform(uniform, v3.Value);
                    break;
                case UniformValueKind.Vec4:
                    if (mv is MaterialValue<Vector4> v4) _gfx.SetUniform(uniform, v4.Value);
                    break;
            }
        }
    }

    public void DrawMeshCommands(ReadOnlySpan<DrawCommandMesh> commands)
    {
        var projView = _view.ProjectionViewMatrix;

        _gfx.BindMesh(_graphics.QuadMeshId);

        foreach (ref readonly var cmd in commands)
        {
            if (_previousMaterial != cmd.MaterialId)
            {
                /*
                var material = _materialStore[cmd.MaterialId];
                _gfx.UseShader(material.ShaderId);
                for (int t = 0; t < material.SamplerSlots.Length; t++)
                {
                    _gfx.BindTexture(material.SamplerSlots[t], (uint)t);
                }
                */
                var material = _materialStore[cmd.MaterialId];
                BindMaterialSlots(material);
                _previousMaterial = cmd.MaterialId;
            }

            _gfx.SetUniform(ShaderUniform.ModelMatrix, in cmd.Transform);
            _gfx.BindMesh(cmd.MeshId);
            _gfx.DrawIndexed(cmd.DrawCount);
        }
    }

    public void RenderLightCommands(LightRenderPass pass, ReadOnlySpan<DrawCommandLight> commands)
    {
        _gfx.UseShader(pass.Shader);
        _gfx.BindMesh(_graphics.QuadMeshId);

        for (int i = 0; i < commands.Length; i++)
        {
            ref readonly var cmd = ref commands[i];

            _gfx.SetUniform(ShaderUniform.LightPos, cmd.Position);
            _gfx.SetUniform(ShaderUniform.Radius, cmd.Radius);
            _gfx.SetUniform(ShaderUniform.Color, cmd.Color);
            _gfx.SetUniform(ShaderUniform.Intensity, cmd.Intensity);
            _gfx.SetUniform(ShaderUniform.Softness, 2.5f);
            _gfx.SetUniform(ShaderUniform.Shape, 0);

            _gfx.Draw();
        }
    }

    public void DrawFullscreenQuad(FsqRenderPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);
        ArgumentNullException.ThrowIfNull(pass.SourceTextures);
        ArgumentOutOfRangeException.ThrowIfZero(pass.SourceTextures.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pass.SourceTextures.Length, 4, nameof(pass.SourceTextures));

        var viewport = _view.ViewportSize;
        _gfx.UseShader(pass.Shader);
        _gfx.SetUniform(ShaderUniform.TexelSize, viewport.ToSystemVec2() * pass.SizeRatio);

        for (int i = 0; i < pass.SourceTextures.Length; i++)
        {
            _gfx.BindTexture(pass.SourceTextures[i], (uint)i);
        }

        _gfx.BindMesh(_graphics.QuadMeshId);
        _gfx.Draw();
    }
}