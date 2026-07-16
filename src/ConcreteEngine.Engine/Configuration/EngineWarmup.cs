using System.Runtime.CompilerServices;
using ConcreteEngine.Editor;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Configuration;

internal static class EngineWarmup
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LoadStaticCtor(GraphicsRuntime graphics)
    {
        graphics.RunStaticCtor();
        EditorPortal.RunStaticCtor();
    }
}