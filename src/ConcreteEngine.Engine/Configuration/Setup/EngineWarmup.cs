using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Configuration.Setup;

internal static class EngineWarmup
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PreWarmup(GraphicsRuntime graphics)
    {
        LoadEnumCache();
        RuntimeHelpers.RunClassConstructor(typeof(EngineMetricHub).TypeHandle);
        graphics.RunStaticCtor();
        EditorPortal.RunStaticCtor();

        YeetGenerics(graphics);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadEnumCache()
    {
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<TimeStepKind>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<EntitySourceKind>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<GraphicsKind>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<AssetKind>).TypeHandle);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void YeetGenerics(GraphicsRuntime graphics)
    {
        graphics.WarmUp();
        Ecs.Warmup();
    }
}