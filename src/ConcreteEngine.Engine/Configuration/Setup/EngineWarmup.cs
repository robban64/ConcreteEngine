using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics;

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
        EditorPortal.WarmUp();
        Ecs.Warmup();
    }
}