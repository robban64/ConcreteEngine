using Silk.NET.Core.Native;
using Silk.NET.OpenGL;

// ReSharper disable HeapView.BoxingAllocation

namespace ConcreteEngine.Graphics.OpenGL;

internal interface IDriverDebugger
{
    void ToggleDebug(bool enabled);
}

internal sealed class GlDebugger : IGraphicsDriverModule, IDriverDebugger
{
    private readonly GL _gl;
    private static DebugProc? _debugProc;

    internal GlDebugger(GL gl)
    {
        _gl = gl;
    }


    public void ToggleDebug(bool enabled)
    {
        if (enabled)
        {
            _gl.Enable(EnableCap.DebugOutput);
            _gl.Enable(EnableCap.DebugOutputSynchronous);
        }
        else
        {
            _gl.Disable(EnableCap.DebugOutput);
            _gl.Disable(EnableCap.DebugOutputSynchronous);
        }
    }

    public unsafe void EnableGlDebug()
    {
        _debugProc = (src, type, id, severity, len, msg, user) =>
        {
            var text = SilkMarshal.PtrToString(msg);
            var srcStr = src.ToString();
            var typeStr = type.ToString();
            var sevStr = severity.ToString();

#if DEBUG
            if (severity == GLEnum.DebugSeverityHigh && Debugger.IsAttached)
                Debugger.Break();
#endif
            Console.WriteLine($"[GL {sevStr}] {typeStr} {id} @ {srcStr}: {text}");
        };

        _gl.Enable(EnableCap.DebugOutput);
        _gl.Enable(EnableCap.DebugOutputSynchronous);
        _gl.DebugMessageCallback(_debugProc, null);
        _gl.DebugMessageControl(GLEnum.DontCare, GLEnum.DontCare, GLEnum.DebugSeverityNotification,
            0, null, false);
    }
}