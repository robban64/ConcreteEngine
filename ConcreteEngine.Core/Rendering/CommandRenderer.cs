using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics;
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
        _materialStore  = materialStore;
    }
    
    
    public void DrawMeshCommands(ReadOnlySpan<DrawCommandMesh> commands)
    {
        var projView = _view.ProjectionViewMatrix;
        
        _gfx.BindMesh(_graphics.QuadMeshId);

        foreach (ref readonly var cmd in commands)
        {
            if (_previousMaterial != cmd.MaterialId)
            {
                var material = _materialStore[cmd.MaterialId];
                _gfx.UseShader(material.Shader.ResourceId);
                _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in projView);
                for (int t = 0; t < material.Textures.Length; t++)
                {
                    _gfx.BindTexture(material.Textures[t].ResourceId, (uint)t);
                }

                _previousMaterial =  cmd.MaterialId;
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
        _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, _view.ProjectionViewMatrix);

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