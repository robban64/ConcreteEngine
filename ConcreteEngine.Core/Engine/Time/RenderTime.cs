using ConcreteEngine.Core.Engine.Data;

namespace ConcreteEngine.Core.Time;

internal sealed class RenderTime
{
    public const int ParticleLodTicksPerSecond = 15; // 15 Hz
    public const int GpuUploadTicksPerSecond = 20; // 20 Hz
    public const int GpuDisposeTicksPerSecond = 1; // 1 Hz

    private readonly FrameTickTimer _renderEffectTicker = new(1f / ParticleLodTicksPerSecond);


    private readonly FrameTickTimer _gpuUploadTicker = new(1f / GpuUploadTicksPerSecond);
    private readonly FrameTickTimer _gpuDisposeTicker = new(1f / GpuDisposeTicksPerSecond);

    private readonly RenderTickDelegate _onRenderEffect;

    private readonly RenderTickDelegate _onGpuUpload;
    private readonly RenderTickDelegate _onGpuDispose;

    private int _a, _b, _c;

    internal RenderTime(RenderTickDelegate onRenderEffect, RenderTickDelegate onGpuUpload,
        RenderTickDelegate onGpuDispose)
    {
        _onRenderEffect = onRenderEffect;
        _onGpuUpload = onGpuUpload;
        _onGpuDispose = onGpuDispose;
    }

    public void Accumulate(float dt)
    {
        _gpuUploadTicker.Accumulate(dt);
        _gpuDisposeTicker.Accumulate(dt);
        _renderEffectTicker.Accumulate(dt);
    }

    public void Advance()
    {
        _a = _gpuUploadTicker.DrainAllTicks();
        _b = _gpuDisposeTicker.DrainAllTicks();
        _c = _renderEffectTicker.DrainAllTicks();

        //if (a > 0) _onGpuUpload(a); 
        //if (b > 0) _onGpuDispose(b);
        //if (c > 0) _onRenderEffect(c);
    }

    public void TickOrGpuUpload()
    {
        if (_a > 0) _onGpuUpload(_a);
    }

    public void TickOrGpuDispose()
    {
        if (_b > 0) _onGpuDispose(_b);
    }

    public void TickOrRenderEffect()
    {
        if (_c > 0) _onRenderEffect(_c);
    }
}