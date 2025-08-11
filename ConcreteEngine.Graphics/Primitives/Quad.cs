namespace ConcreteEngine.Graphics.Primitives;

public class Quad
{
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
}