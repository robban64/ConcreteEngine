using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Loader.Importer;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpMaterial = Silk.NET.Assimp.Material;
using AssimpTexture = Silk.NET.Assimp.Texture;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterModel;

internal sealed unsafe partial class ModelImporter
{
    public void ProcessSceneMaterials(AssimpScene* scene)
    {
        int matCount = (int)scene->MNumMaterials, texCount = (int)scene->MNumTextures;

        if (matCount == 0 || texCount == 0) return;
        
        // register textures
        for (var i = 0; i < texCount; i++)
        {
            var aiTexture = scene->MTextures[i];
            var embeddedName = aiTexture->MFilename.AsString;
            var assetName = $"{Ctx.ModelName}::Textures/{i}";
            var texture = new EmbeddedSceneTexture(assetName, embeddedName, i);
            Ctx.Textures.Add(texture);
        }

        for (var i = 0; i < matCount; i++)
        {
            var aiMat = scene->MMaterials[i];
            var assetName = $"{Ctx.ModelName}::Materials/{i}";
            var material = new EmbeddedSceneMaterial(assetName, i, Ctx.Animation != null);
            ProcessMaterialProperties(aiMat, material);
            
            material.FileSpec = new AssetFileSpec(
                GId: Guid.NewGuid(),
                Id: new AssetFileId(0),
                Storage: AssetStorageKind.Embedded,
                RelativePath: assetName,
                LogicalName: material.EmbeddedName,
                LastWriteTime: DateTime.MinValue,
                SizeBytes: 0,
                Source: Ctx.Filename);

            Ctx.Materials.Add(material);
        }

        LoadTextures(scene);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadTextures(AssimpScene* scene)
    {
        var textures = Ctx.Textures;
        for (var i = 0; i < textures.Count; i++)
        {
            if(textures[i].Discard) textures.RemoveAt(i);
        }

        foreach (var texture in textures)
        {
            var aiTexture = scene->MTextures[texture.TextureIndex];
            texture.PixelData = MatUtils.LoadTextureData(aiTexture, texture.PixelFormat, out texture.Dimensions);
            texture.FileSpec = new AssetFileSpec(
                GId: Guid.NewGuid(),
                Id: new AssetFileId(0),
                Storage: AssetStorageKind.Embedded,
                RelativePath: texture.Name,
                LogicalName: texture.EmbeddedName,
                SizeBytes: texture.PixelData.Length,
                LastWriteTime: DateTime.MinValue,
                Source: Ctx.Filename);
        }
    }

    private static void ProcessMaterialProperties(AssimpMaterial* aiMat, EmbeddedSceneMaterial material)
    {
        Span<char> charBuffer = stackalloc char[256];
        Span<char> keyCharBuffer = stackalloc char[64];

        MaterialParams matData = default;

        for (var i = 0; i < aiMat->MNumProperties; i++)
        {
            var prop = aiMat->MProperties[i];
            var keyBytes = new Span<byte>(prop->MKey.Data, (int)prop->MKey.Length);
            var written = Encoding.UTF8.GetChars(keyBytes, keyCharBuffer);
            var key = keyCharBuffer.Slice(0, written);

            switch (key)
            {
                case "?mat.name":
                    material.EmbeddedName = MatUtils.ParsePropertyString(prop, charBuffer).ToString();
                    break;
                case "$tex.file":
                    var texturePath = MatUtils.ParsePropertyString(prop, charBuffer);
                    AttachTextureToMaterial(material, texturePath, (TextureType)prop->MSemantic);
                    break;
                case "$mat.opacity":
                    MatUtils.ParseFloatProp(prop, out matData.Color.A);
                    break;
                case Assimp.MaterialShininessBase:
                    MatUtils.ParseFloatProp(prop, out matData.Shininess);
                    break;
                case Assimp.MatkeySpecularFactor:
                    MatUtils.ParseFloatProp(prop, out matData.Specular);
                    break;
                case Assimp.MaterialColorDiffuseBase:
                    MatUtils.ParseVectorProp(prop, out Unsafe.As<Color4, Vector4>(ref matData.Color));
                    break;
            }
        }

        material.Params = matData;
    }

    private static void AttachTextureToMaterial(EmbeddedSceneMaterial material, Span<char> texturePath,
        TextureType type)
    {
        if (!texturePath.StartsWith("*") || texturePath.Length < 2)
            throw new ArgumentException($"Invalid texture path {texturePath}", nameof(texturePath));

        var textures = Ctx.Textures;

        var id = texturePath[1..];
        if (!int.TryParse(id, out var textureIndex) || (uint)textureIndex > textures.Count)
            throw new InvalidOperationException($"Invalid texture index {id}");

        var texture = textures[textureIndex];
        if (textures[textureIndex].TextureIndex != textureIndex)
        {
            throw new InvalidOperationException(
                $"Property texture index {textureIndex} does not match {texture.TextureIndex}");
        }
        
        if(material.Textures.Contains((texture.GId,textureIndex))) return;
        if (!MatUtils.ToSystemEnums(type, out var kind, out var format))
        {
            texture.Discard = true;
            return;
        }
        texture.SlotKind = kind;
        texture.PixelFormat = format;
        texture.Discard = false;
        material.Textures.Add((texture.GId, textureIndex));
    }
}

file static unsafe class MatUtils
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static byte[] LoadTextureData(AssimpTexture* texture, TexturePixelFormat format, out Size2D size)
    {
        int width = (int)texture->MWidth, height = (int)texture->MHeight;

        int sizeInBytes;
        // Compressed mode (PNG, JPG)
        // width = file size in bytes
        if (height == 0)
        {
            sizeInBytes = width;
        }
        // raw mode (BGRA8888)
        // standard: width height and 4 bytes per pixel
        else
        {
            sizeInBytes = width * height * 4;
        }

        InvalidOpThrower.ThrowIf(sizeInBytes < 4, nameof(sizeInBytes));
        var ptr = (byte*)texture->PcData;

        return TextureImporter.ImportUnmanagedTexture(ptr, sizeInBytes, width, height, format, out size);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ToSystemEnums(TextureType type, out TextureUsage kind, out TexturePixelFormat format)
    {
        switch (type)
        {
            case TextureType.Diffuse:
                kind = TextureUsage.Albedo;
                format = TexturePixelFormat.SrgbAlpha;
                return true;
            case TextureType.Normals:
                kind = TextureUsage.Normal;
                format = TexturePixelFormat.Rgba;
                return true;
            case TextureType.Opacity:
                kind = TextureUsage.Mask;
                format = TexturePixelFormat.Red;
                return true;
            default:
                kind = 0;
                format = 0;
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ParseFloatProp(MaterialProperty* prop, out float value)
    {
        if (prop == null || prop->MData == null || prop->MDataLength < sizeof(float))
            throw new ArgumentOutOfRangeException(nameof(prop));

        value = MemoryMarshal.Read<float>(new ReadOnlySpan<byte>(prop->MData, (int)prop->MDataLength));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ParseVectorProp(MaterialProperty* prop, out Vector4 data)
    {
        var length = (int)prop->MDataLength;
        if (length != 16 && length != 12)
            throw new ArgumentOutOfRangeException(nameof(prop));

        ref var b0 = ref Unsafe.AsRef<byte>(prop->MData);
        var span = MemoryMarshal.CreateSpan(ref b0, length);

        data = length == 16 ? MemoryMarshal.Read<Vector4>(span) : MemoryMarshal.Read<Vector3>(span).AsVector4();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Span<char> ParsePropertyString(MaterialProperty* prop, Span<char> charBuffer)
    {
        if (prop->MType != PropertyTypeInfo.String)
            throw new ArgumentOutOfRangeException(nameof(prop));

        // 4-byte integer followed by the string
        var length = *(int*)prop->MData;
        var stringStartPtr = prop->MData + 4;

        if (length <= 0 || length >= prop->MDataLength)
            return Span<char>.Empty;

        var keyBytes = new Span<byte>(stringStartPtr, length);
        var written = Encoding.UTF8.GetChars(keyBytes, charBuffer);
        return charBuffer.Slice(0, written);
    }
}