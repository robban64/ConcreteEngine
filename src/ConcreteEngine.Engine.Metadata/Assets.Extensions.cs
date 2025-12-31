namespace ConcreteEngine.Engine.Metadata;

public static class AssetsExtensions
{
    extension(AssetKind kind)
    {
        public string ToText()
        {
            return kind switch
            {
                AssetKind.Unknown => "Unknown",
                AssetKind.Shader => "Shader",
                AssetKind.Model => "Model",
                AssetKind.Texture2D => "Texture2D",
                AssetKind.TextureCubeMap => "CubeMap",
                AssetKind.MaterialTemplate => "MaterialTemplate",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        public string ToShortText()
        {
            return kind switch
            {
                AssetKind.Unknown => "INV",
                AssetKind.Shader => "SHD",
                AssetKind.Model => "MOD",
                AssetKind.Texture2D => "TEX",
                AssetKind.TextureCubeMap => "TEX-C",
                AssetKind.MaterialTemplate => "MAT-T",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}