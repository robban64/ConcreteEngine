#region

#endregion

namespace ConcreteEngine.Engine.Time;

internal sealed class RenderTickScheduler
{
    private struct RenderFrameTicker(float targetFrameMs)
    {
        private int _frameCounter = 0;
        private float _accumulator = 0f;

        public bool TryProcessFrame(float dt)
        {
            _frameCounter++;
            _accumulator += dt;
            if (_accumulator < targetFrameMs) return false;
            _frameCounter = 0;
            _accumulator = 0;
            return true;
        }
    }

    //public const int ParticleLodTicksPerSecond = 15;
    public const int GuiRenderFramePerSecond = 30;
    public const int MetricsFramePerSecond = 4; // 4 Hz
    public const int LogFramePerSecond = 15;

    //private RenderFrameTicker _renderEffectTicker = new(1f / ParticleLodTicksPerSecond);
    private RenderFrameTicker _guiTicker = new(1f / GuiRenderFramePerSecond);
    private RenderFrameTicker _metricsTicker = new(1f / MetricsFramePerSecond);
    private RenderFrameTicker _logTicker = new(1f / LogFramePerSecond);


    public void TryRenderGui(float dt) => _guiTicker.TryProcessFrame(dt);
    public bool TryProcessMetrics(float dt) => _metricsTicker.TryProcessFrame(dt);
    public bool TryProcessLoggers(float dt) => _logTicker.TryProcessFrame(dt);
}