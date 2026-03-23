using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Configuration.Setup;

internal static class EngineWarmup
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LoadStaticCtor(GraphicsRuntime graphics)
    {
        LoadEnumCache();
        graphics.RunStaticCtor();
        EditorPortal.RunStaticCtor();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadEnumCache()
    {
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<TimeStepKind>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<EntitySourceKind>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<GraphicsKind>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<AssetKind>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<SceneObjectKind>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<BlendMode>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<CullMode>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<DepthMode>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<TextureUsage>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<PolygonOffsetLevel>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<TexturePreset>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<TextureAnisotropy>).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(EnumCache<TexturePixelFormat>).TypeHandle);

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void YeetGenerics(GraphicsRuntime graphics)
    {
        graphics.Warmup();
        Ecs.Internals.Warmup();
    }
}