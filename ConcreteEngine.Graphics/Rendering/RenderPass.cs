#region

using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Rendering;

public sealed class RenderPass
{
    private readonly RenderTargetId _id;
    private readonly IRenderTarget? _renderTarget;

    private readonly List<IDrawCommand> _commands = new (64);

    private readonly HashSet<ushort> _usedShaders = new(8);

    private Matrix4X4<float> _projectionViewMatrix;

    public Matrix4X4<float> ProjectionViewMatrix
    {
        get => _projectionViewMatrix;
        set => _projectionViewMatrix = value;
    }

    public bool HasData => _commands.Count > 0;
    public RenderTargetId Id => _id;

    public RenderPass(RenderTargetId id, IRenderTarget? renderTarget)
    {
        _id = id;
        _renderTarget = renderTarget;
    }


    public void AddCommand<T>(T cmd) where T : IDrawCommand
    {
        _commands.Add(cmd);
        _usedShaders.Add(cmd.ShaderId);
    }

    public void Execute(IGraphicsContext ctx)
    {
        //renderTarget.Bind();
        foreach (var shader in _usedShaders)
        {
            ctx.UseShader(shader);
            ctx.SetUniform(ShaderUniform.ProjectionViewMatrix, in _projectionViewMatrix);
        }

        ctx.UseShader(0);

        foreach (var cmd in _commands)
        {
            cmd.Execute(ctx);
        }

        _commands.Clear();
        _usedShaders.Clear();
    }
}