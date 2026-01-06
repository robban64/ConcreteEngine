namespace ConcreteEngine.Core.Engine.Assets.Utils;

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
                AssetKind.Texture => "Texture",
                AssetKind.Material => "MaterialTemplate",
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
                AssetKind.Texture => "TEX",
                AssetKind.Material => "MAT-T",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}