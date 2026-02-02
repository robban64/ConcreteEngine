namespace ConcreteEngine.Core.Engine.Assets;

public interface IModel : IAsset
{
    int VertexCount { get; init; }
    int FaceCount { get; init; }
    int MeshCount { get; }
    bool IsAnimated { get; }
}