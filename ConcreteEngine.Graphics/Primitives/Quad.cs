namespace ConcreteEngine.Graphics.Primitives;

internal static class Quad
{
    public static readonly Vertex2D[] Vertices =
    {
        // pos     // uv
        new(-1f, -1f, 0f, 0f),
        new(1f, -1f, 1f, 0f),
        new(-1f, 1f, 0f, 1f),
        new(1f, 1f, 1f, 1f)
    };


    /*
    private static readonly float[] Vertices =
    [
//       aPosition     | aTexCoords
        0.5f, 0.5f, 1.0f, 1.0f,
        0.5f, -0.5f, 1.0f, 0.0f,
        -0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, 0.5f, 0.0f, 1.0f
    ];

    private static readonly uint[] Indices =
    [
        0u, 3u, 1u,
        1u, 3u, 2u
    ];
    */
}