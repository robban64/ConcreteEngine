using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets;

internal static class AssetFactory
{

    public static Mesh MakeMesh(AssetFinalEntry<MeshManifestRecord, MeshCreationInfo, MeshId> entry)
    {
        return new Mesh
        {
            Name = entry.Record.Name,
            Filename = entry.Record.Filename,
            DrawCount = entry.Descriptor.DrawCount,
            ResourceId = entry.ResourceId
        };
    }

    public static Texture2D MakeTexture(AssetFinalEntry<TextureManifestRecord, TextureCreationInfo, TextureId> entry)
    {
        var texture = new Texture2D
        {
            Name = entry.Record.Name,
            Path = entry.Record.Filename,
            ResourceId = entry.ResourceId,
            Width = entry.Descriptor.Width,
            Height = entry.Descriptor.Height,
            PixelFormat = entry.Descriptor.PixelFormat,
            Preset = entry.Record.Preset,
            Anisotropy = entry.Record.Anisotropy,
        };
        if (entry.Descriptor.Data is { } tData)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(tData.Length, 0);
            texture.SetPixelData(tData);
        }
        return texture;
    }

    public static CubeMap MakeCubeMap(AssetFinalEntry<CubeMapManifestRecord, CubeMapCreationInfo, TextureId> entry)
    {
        return new CubeMap
        {
            Name = entry.Record.Name,
            ResourceId = entry.ResourceId,
            Textures = entry.Record.Textures,
            Width = entry.Record.Width,
            Height = entry.Record.Height,
            PixelFormat = entry.Descriptor.PixelFormat
        };
    }

    public static Shader MakeShader(AssetFinalEntry<ShaderManifestRecord, ShaderCreationInfo, ShaderId> entry)
    {
        return new Shader
        {
            Name = entry.Record.Name,
            FragShaderFilename = entry.Record.FragmentFilename,
            VertShaderFilename = entry.Record.VertexFilename,
            ResourceId = entry.ResourceId,
            Samplers = entry.Descriptor.Samplers
        };
    }
}