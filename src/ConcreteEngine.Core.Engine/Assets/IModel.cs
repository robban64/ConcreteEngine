namespace ConcreteEngine.Core.Engine.Assets;

public interface IModel : IAsset
{
    int DrawCount { get; }
    int MeshCount { get; }
    bool IsAnimated { get; }
}