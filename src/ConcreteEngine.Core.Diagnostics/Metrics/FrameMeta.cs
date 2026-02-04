namespace ConcreteEngine.Core.Diagnostics.Metrics;

public struct GpuFrameMetaBundle(in GpuBufferMeta buffer, in RenderFrameMeta frame)
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

public struct RenderFrameMeta(int draws, int tris, int instances)
{
    public int Draws = draws;
    public int Tris = tris;
    public int Instances = instances;

    public void AddDrawCall(int tris)
    {
        Draws++;
        Tris += tris;
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