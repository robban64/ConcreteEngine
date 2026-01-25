namespace ConcreteEngine.Core.Engine.Assets;

public interface IMaterial : IAsset
{
    AssetId TemplateId { get; }
    AssetId AssetShader { get; }
}