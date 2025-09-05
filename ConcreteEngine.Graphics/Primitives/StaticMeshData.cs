namespace ConcreteEngine.Graphics.Primitives;

internal static class StaticMeshData
{
    public static readonly Vertex2D[] FsqQuadVertices =
    {
        new(-1f, -1f, 0f, 0f),
        new(1f, -1f, 1f, 0f),
        new(-1f, 1f, 0f, 1f),
        new(1f, 1f, 1f, 1f)
    };
    
    public static float[] SkyboxVertices =
    {
        // +X
        1f,  1f, -1f,  1f, -1f, -1f,  1f, -1f,  1f,
        1f,  1f, -1f,  1f, -1f,  1f,  1f,  1f,  1f,
        // -X
        -1f,  1f,  1f, -1f, -1f,  1f, -1f, -1f, -1f,
        -1f,  1f,  1f, -1f, -1f, -1f, -1f,  1f, -1f,
        // +Y
        -1f,  1f, -1f,  1f,  1f, -1f,  1f,  1f,  1f,
        -1f,  1f, -1f,  1f,  1f,  1f, -1f,  1f,  1f,
        // -Y
        -1f, -1f,  1f,  1f, -1f,  1f,  1f, -1f, -1f,
        -1f, -1f,  1f,  1f, -1f, -1f, -1f, -1f, -1f,
        // +Z
        -1f,  1f,  1f,  1f,  1f,  1f,  1f, -1f,  1f,
        -1f,  1f,  1f,  1f, -1f,  1f, -1f, -1f,  1f,
        // -Z
        1f,  1f, -1f, -1f,  1f, -1f, -1f, -1f, -1f,
        1f,  1f, -1f, -1f, -1f, -1f,  1f, -1f, -1f,
    };


}