#region

using ConcreteEngine.Core.Data;

#endregion

namespace ConcreteEngine.Core.Time;

internal sealed class RenderTickScheduler
{
    public const int ParticleLodTicksPerSecond = 15;
    public const int GpuUploadTicksPerSecond = 20;
    public const int GpuDisposeTicksPerSecond = 1; // 1 Hz

    private readonly FrameTickTimer _renderEffectTicker = new(1f / ParticleLodTicksPerSecond);


    private readonly FrameTickTimer _gfxUploadTicker = new(1f / GpuUploadTicksPerSecond);
    private readonly FrameTickTimer _gfxDisposeTicker = new(1f / GpuDisposeTicksPerSecond);

    private readonly RenderTickDelegate _onGfxUpload;
    private readonly RenderTickDelegate _onGfxDispose;

    private int _a, _b, _c;

    internal RenderTickScheduler(RenderTickDelegate onGfxUpload, RenderTickDelegate onGfxDispose)
    {
        _onGfxUpload = onGfxUpload;
        _onGfxDispose = onGfxDispose;
    }

    public void Accumulate(float dt)
    {
        /*
        _gfxUploadTicker.Accumulate(dt);
        _gfxDisposeTicker.Accumulate(dt);
        _renderEffectTicker.Accumulate(dt);
        */
    }

    public void Advance()
    {
        /*
        _a = _gfxUploadTicker.DrainAllTicks();
        _b = _gfxDisposeTicker.DrainAllTicks();
        _c = _renderEffectTicker.DrainAllTicks();
        */
        //if (a > 0) _onGpuUpload(a); 
        //if (b > 0) _onGpuDispose(b);
        //if (c > 0) _onRenderEffect(c);
    }
}