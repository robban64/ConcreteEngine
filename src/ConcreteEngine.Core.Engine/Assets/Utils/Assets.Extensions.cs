namespace ConcreteEngine.Core.Engine.Assets.Utils;

public static class AssetsExtensions
{
    extension(AssetKind kind)
    {
        public ReadOnlySpan<byte> ToTextUtf8()
        {
            return kind switch
            {
                AssetKind.Unknown => "Unknown"u8,
                AssetKind.Shader => "Shader"u8,
                AssetKind.Model => "Model"u8,
                AssetKind.Texture => "Texture"u8,
                AssetKind.Material => "Material"u8,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        public string ToText()
        {
            return kind switch
            {
                AssetKind.Unknown => "Unknown",
                AssetKind.Shader => "Shader",
                AssetKind.Model => "Model",
                AssetKind.Texture => "Texture",
                AssetKind.Material => "Material",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        public ReadOnlySpan<byte> ToShortTextUtf8()
        {
            return kind switch
            {
                AssetKind.Unknown => "INV"u8,
                AssetKind.Shader => "SHD"u8,
                AssetKind.Model => "MOD"u8,
                AssetKind.Texture => "TEX"u8,
                AssetKind.Material => "MAT"u8,
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
                AssetKind.Material => "MAT",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}