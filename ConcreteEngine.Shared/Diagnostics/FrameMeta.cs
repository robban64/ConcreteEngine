namespace ConcreteEngine.Shared.Diagnostics;


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

public readonly struct GpuBufferMeta(int texturesMb, int meshBufferMb, int uniformBufferMb)
{
    public readonly int TexturesMb = texturesMb;
    public readonly int MeshBufferMb = meshBufferMb;
    public readonly int UniformBufferMb = uniformBufferMb;
}

public readonly struct SceneMeta(int sceneObjects, int gameEcs, int renderEcs, int visibleEntities)
{
    public readonly int SceneObjects = sceneObjects;
    public readonly int GameEcs = gameEcs;
    public readonly int RenderEcs = renderEcs;
    public readonly int VisibleEntities = visibleEntities;
}

