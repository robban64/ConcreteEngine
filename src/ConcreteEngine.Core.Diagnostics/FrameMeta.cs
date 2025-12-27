namespace ConcreteEngine.Core.Diagnostics;

public struct FrameMetaBundle
{
    public FrameMeta Frame;
    public RenderFrameMeta RenderFrame;
}

public readonly struct FrameMeta(long frameId, float fps, float alpha)
{
    public readonly long FrameId = frameId;
    public readonly float Fps = fps;
    public readonly float Alpha = alpha;
}

public readonly struct RenderFrameMeta(int draws, int tris, int instances)
{
    public readonly int Draws = draws;
    public readonly int Tris = tris;
    public readonly int Instances = instances;
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