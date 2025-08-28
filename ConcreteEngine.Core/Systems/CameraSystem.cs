#region

using System.Numerics;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Input;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Systems;


public interface ICameraSystem: IGameEngineSystem
{
    public GameCamera Camera { get; }

}
public sealed class CameraSystem :ICameraSystem
{
    private const int EdgeMarginPixels = 16;
    private const float BaseSpeed = 200;

    private readonly IEngineInputSource _input;
    private readonly GameCamera _camera;

    public GameCamera Camera => _camera;

    internal CameraSystem(IEngineInputSource input)
    {
        _input = input;
        _camera = new GameCamera();
    }

    public void Update(in FrameMetaInfo frameCtx)
    {
        _camera.SetViewport(frameCtx.ViewportSize);
    }

    public void Shutdown()
    {
    }

}


