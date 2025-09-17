using ConcreteEngine.Graphics.Error;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlDebugger
{
    private readonly GL _gl;
    private static DebugProc? _debugProc;

    internal GlDebugger(GL gl)
    {
        _gl = gl;
    }

    public void CheckGlError()
    {
        var error = _gl.GetError();
        if (error != (GLEnum)ErrorCode.NoError)
            throw new OpenGlException(error);
    }

    public unsafe void EnableGlDebug()
    {
        _debugProc = (src, type, id, severity, len, msg, user) =>
        {
            var text = SilkMarshal.PtrToString((nint)msg);
            Console.WriteLine($"[GL {severity}] {type} {id}: {text}");
        };

        _gl.Enable(EnableCap.DebugOutput);
        _gl.Enable(EnableCap.DebugOutputSynchronous);
        _gl.DebugMessageCallback(_debugProc, null);
        _gl.DebugMessageControl(GLEnum.DontCare, GLEnum.DontCare, GLEnum.DebugSeverityNotification,
            0, null, false);


        _gl.Enable(EnableCap.DebugOutput);
        _gl.Enable(EnableCap.DebugOutputSynchronous);
        _gl.DebugMessageCallback(_debugProc, null);
        _gl.DebugMessageControl(GLEnum.DontCare, GLEnum.DontCare, GLEnum.DebugSeverityNotification, 0, null, false);
    }
}