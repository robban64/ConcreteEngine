#if DEBUG
using System.Diagnostics;
#endif

using Silk.NET.Core.Native;
using Silk.NET.OpenGL;

// ReSharper disable HeapView.BoxingAllocation

namespace ConcreteEngine.Graphics.OpenGL;

internal interface IDriverDebugger
{
    void ToggleDebug(bool enabled);
}

internal sealed class GlDebugger : IDriverDebugger
{
    private static GL Gl => GlBackendDriver.Gl;
    private static DebugProc? _debugProc;


    public void ToggleDebug(bool enabled)
    {
        if (enabled)
        {
            Gl.Enable(EnableCap.DebugOutput);
            Gl.Enable(EnableCap.DebugOutputSynchronous);
        }
        else
        {
            Gl.Disable(EnableCap.DebugOutput);
            Gl.Disable(EnableCap.DebugOutputSynchronous);
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

        Gl.Enable(EnableCap.DebugOutput);
        Gl.Enable(EnableCap.DebugOutputSynchronous);
        Gl.DebugMessageCallback(_debugProc, null);
        Gl.DebugMessageControl(GLEnum.DontCare, GLEnum.DontCare, GLEnum.DebugSeverityNotification,
            0, null, false);
    }
}