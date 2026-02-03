namespace ConcreteEngine.Core.Engine.Assets;

public interface IModel : IAsset
{
    int VertexCount { get; }
    int FaceCount { get; }
    int MeshCount { get; }
    bool IsAnimated { get; }
}