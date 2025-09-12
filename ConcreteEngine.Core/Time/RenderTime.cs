namespace ConcreteEngine.Core.Time;

public sealed class RenderTime
{
    public const int ParticleLodTicksPerSecond = 15; // 15 Hz
    public const int GpuUploadTicksPerSecond = 20; // 20 Hz
    public const int GpuDisposeTicksPerSecond = 1; // 1 Hz

    private readonly FrameTickTimer _renderEffectTicker = new(1f / ParticleLodTicksPerSecond);


    private readonly FrameTickTimer _gpuUploadTicker = new(1f / GpuUploadTicksPerSecond);
    private readonly FrameTickTimer _gpuDisposeTicker = new(1f / GpuDisposeTicksPerSecond);

    private readonly GameTimeTickDelegate _onRenderEffect;

    private readonly GameTimeTickDelegate _onGpuUpload;
    private readonly GameTimeTickDelegate _onGpuDispose;

    private int a, b, c;

    internal RenderTime(GameTimeTickDelegate onRenderEffect, GameTimeTickDelegate onGpuUpload, GameTimeTickDelegate onGpuDispose)
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
        a = _gpuUploadTicker.DrainAllTicks();
        b = _gpuDisposeTicker.DrainAllTicks();
        c = _renderEffectTicker.DrainAllTicks();

        //if (a > 0) _onGpuUpload(a); 
        //if (b > 0) _onGpuDispose(b);
        //if (c > 0) _onRenderEffect(c);
    }

    public void TickOrGpuUpload()
    {
        if (a > 0) _onGpuUpload(a); 
    }
    
    public void TickOrGpuDispose()
    {
        if (b > 0) _onGpuDispose(b);
    }
    
    public void TickOrRenderEffect()
    {
        if (c > 0) _onRenderEffect(c);
    }

}