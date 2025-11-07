#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Meshes;

#endregion

namespace ConcreteEngine.Engine.Assets.Descriptors;

internal record AssetModelData(AssetId AssetId, string Name, MeshPartData[] MeshData) : IAssetData
{
    public AssetKind Kind => AssetKind.Model;
}

internal record AssetShaderData(AssetId AssetId, string Name, string VertexShader, string FragmentShader) : IAssetData
{
    public AssetKind Kind => AssetKind.Shader;
}