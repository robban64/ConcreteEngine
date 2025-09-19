#region

using System.Numerics;
using ConcreteEngine.Core.Assets;

#endregion

namespace ConcreteEngine.Core.Resources;



public sealed class MaterialTemplate : IAssetFile
{
    public required string Name { get; init; }
    public required Shader Shader { get; set; }
    public Texture2D[] Textures { get; init; } = [];
    public CubeMap? CubeMap { get; init; } = null;

    public Vector4 Color { get; set; } = Vector4.One;

    public AssetFileType AssetType => AssetFileType.Material;

    internal MaterialTemplate()
    {

    }

}