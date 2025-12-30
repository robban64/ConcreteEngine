using System.Runtime.CompilerServices;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Configuration;

internal static class EngineWarmup
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void YeetGenerics(GraphicsRuntime graphics)
    {
        graphics.WarmUp();
        Ecs.Warmup();
        EditorPortal.WarmUp();

        RuntimeHelpers.RunClassConstructor(typeof(EngineMetricHub).TypeHandle);
    }
}