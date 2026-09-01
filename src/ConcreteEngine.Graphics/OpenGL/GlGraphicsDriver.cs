using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Configuration;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlDriver
{
    private static GlDriver _instance = null!;

    public static GL Gl
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _instance._gl;
    }

    public static void Make(GlStartupConfig config)
    {
        if (_instance != null!) throw new InvalidOperationException("Driver already created.");
        _instance = new GlDriver(config.DriverContext);
    }

    private readonly GL _gl;

    private GlDriver(GL gl)
    {
        ArgumentNullException.ThrowIfNull(gl);
        _gl = gl;
    }
}

internal static class GlBackendDriver
{
    public static GlCapabilities Capabilities { get; private set; } = null!;

    private static DebugProc? _debugProc;

    internal static GlCapabilities Initialize(GlStartupConfig config)
    {
        GlDriver.Make(config);
        if (Capabilities != null!) throw new InvalidOperationException("Gl already initialized");

        Capabilities = new GlCapabilities();
        Capabilities.CreateDeviceCapabilities(GlDriver.Gl);
        EnableGlDebug();

        GlDriver.Gl.Enable(GLEnum.Dither);
        GlDriver.Gl.Enable(GLEnum.Multisample);
        GlDriver.Gl.Enable(EnableCap.TextureCubeMapSeamless);
        GlDriver.Gl.PixelStore(GLEnum.UnpackAlignment, 1);

        GlDriver.Gl.DepthMask(true);

        GlDriver.Gl.Enable(EnableCap.CullFace);
        GlDriver.Gl.CullFace(TriangleFace.Back);
        GlDriver.Gl.FrontFace(FrontFaceDirection.Ccw);

        return Capabilities;
    }


    public static void ToggleDebug(bool enabled)
    {
        if (enabled)
        {
            GlDriver.Gl.Enable(EnableCap.DebugOutput);
            GlDriver.Gl.Enable(EnableCap.DebugOutputSynchronous);
        }
        else
        {
            GlDriver.Gl.Disable(EnableCap.DebugOutput);
            GlDriver.Gl.Disable(EnableCap.DebugOutputSynchronous);
        }
    }

    public static unsafe void EnableGlDebug()
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

        GlDriver.Gl.Enable(EnableCap.DebugOutput);
        GlDriver.Gl.Enable(EnableCap.DebugOutputSynchronous);
        GlDriver.Gl.DebugMessageCallback(_debugProc, null);
        GlDriver.Gl.DebugMessageControl(GLEnum.DontCare, GLEnum.DontCare, GLEnum.DebugSeverityNotification,
            0, null, false);
    }
}