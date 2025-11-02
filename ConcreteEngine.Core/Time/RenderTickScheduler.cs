#region

using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Time.Tickers;

#endregion

namespace ConcreteEngine.Core.Time;

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
    public const int DiagnosticFramePerSecond = 4; // 4 Hz
    
    //private RenderFrameTicker _renderEffectTicker = new(1f / ParticleLodTicksPerSecond);
    private RenderFrameTicker _guiTicker = new(1f / GuiRenderFramePerSecond);
    private RenderFrameTicker _diagnosticTicker = new(1f / DiagnosticFramePerSecond);


    public void TryRenderGui(float dt) => _guiTicker.TryProcessFrame(dt);
    public bool TryProcessDiagnostic(float dt) => _diagnosticTicker.TryProcessFrame(dt);
    
}