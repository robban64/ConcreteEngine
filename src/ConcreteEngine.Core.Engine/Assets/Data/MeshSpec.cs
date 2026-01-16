namespace ConcreteEngine.Core.Engine.Assets;

public readonly struct MeshSpec(int meshIndex, int materialIndex, int drawCount)
{
    public readonly int DrawCount = drawCount;
    public readonly int MeshIndex = meshIndex;
    public readonly int MaterialIndex = materialIndex;
}