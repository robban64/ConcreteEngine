using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Metrics;

public struct GpuFrameMeta(in GpuBufferMeta buffer, in RenderFrameMeta frame)
{
    public GpuBufferMeta Buffer = buffer;
    public RenderFrameMeta Frame = frame;
}

public readonly struct FrameMeta(long frameId, float fps, float alpha)
{
    public readonly long FrameId = frameId;
    public readonly float Fps = fps;
    public readonly float Alpha = alpha;
}

public struct RenderFrameMeta(uint draws, uint tris, uint instances)
{
    public uint Draws = draws;
    public uint Tris = tris;
    public uint Instances = instances;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddDrawCall(uint tris, uint instances)
    {
        Draws++;
        Tris += tris;
        Instances += instances;
    }
}

public readonly struct GpuBufferMeta(long meshBufferBytes, long uniformBufferBytes)
{
    public readonly long MeshBufferBytes = meshBufferBytes;
    public readonly long UniformBufferBytes = uniformBufferBytes;
}

public readonly struct SceneMeta(int sceneObjects, int visibleEntities, int gameEcs, int renderEcs)
{
    public readonly int SceneObjects = sceneObjects;
    public readonly int VisibleEntities = visibleEntities;
    public readonly int GameEcs = gameEcs;
    public readonly int RenderEcs = renderEcs;
}