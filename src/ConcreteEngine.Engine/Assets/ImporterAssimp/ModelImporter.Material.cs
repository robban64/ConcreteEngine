using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;
using Silk.NET.Assimp;
using AssimpMaterial = Silk.NET.Assimp.Material;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpTexture = Silk.NET.Assimp.Texture;
using BlendMode = ConcreteEngine.Graphics.Gfx.BlendMode;

namespace ConcreteEngine.Engine.Assets.ImporterAssimp;

internal static unsafe class MaterialModelImporter
{
    public static void ProcessMaterials(Assimp assimp, AssimpScene* scene, ModelImportContext ctx)
    {
        var embeddedContext = ctx.EmbeddedContext;
        if (embeddedContext.MaterialCount == 0 && embeddedContext.TextureCount == 0) return;
        
        int textureCount = embeddedContext.TextureCount, materialCount = embeddedContext.MaterialCount;
        // register textures
        for (var i = 0; i < textureCount; i++)
        {
            var embeddedName = scene->MTextures[i]->MFilename.AsString;
            var assetName = AssetNameUtils.MakeEmbeddedName(AssetKind.Texture, ctx.ModelName!, i);
            var texture = new EmbeddedSceneTexture(assetName, embeddedName, i);
            embeddedContext.Textures.Add(texture);
        }

        for (var i = 0; i < materialCount; i++)
        {
            var assetName = AssetNameUtils.MakeEmbeddedName(AssetKind.Material, ctx.ModelName!, i);
            var aiMat = scene->MMaterials[i];

            var material = new EmbeddedSceneMaterial(assetName, i, ctx.IsAnimated);
            ProcessMaterialProperties(aiMat, material, ctx);

            material.FileSpec = new AssetFile(
                GId: Guid.NewGuid(),
                Id: AssetFileId.Empty,
                Binding: FileBinding.RootFile,
                Storage: AssetStorage.Embedded,
                RelativePath: assetName,
                LogicalName: material.EmbeddedName,
                LastWriteTime: DateTime.MinValue,
                SizeBytes: 0,
                Source: ctx.Filename);

            embeddedContext.Materials.Add(material);
        }

        LoadTextures(scene, ctx);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadTextures(AssimpScene* scene, ModelImportContext ctx)
    {
        var textures = ctx.EmbeddedContext.Textures;
        foreach (var texture in textures)
        {
            var aiTexture = scene->MTextures[texture.TextureIndex];
            int textureSize = LoadTextureData(ctx.EmbeddedContext, aiTexture, texture);
            texture.FileSpec = new AssetFile(
                GId: Guid.NewGuid(),
                Id: AssetFileId.Empty,
                Binding: FileBinding.RootFile,
                Storage: AssetStorage.Embedded,
                RelativePath: texture.Name,
                LogicalName: texture.EmbeddedName,
                SizeBytes: textureSize,
                LastWriteTime: DateTime.MinValue,
                Source: ctx.Filename);
        }
    }

    private static void ProcessMaterialProperties(AssimpMaterial* aiMat, EmbeddedSceneMaterial material,
        ModelImportContext ctx)
    {
        var matParams = material.State;
        Span<char> charBuffer = stackalloc char[256];
        Span<char> keyCharBuffer = stackalloc char[64];
        var propCount = aiMat->MNumProperties;
        for (var i = 0; i < propCount; i++)
        {
            var prop = aiMat->MProperties[i];
            var keyBytes = new Span<byte>(prop->MKey.Data, (int)prop->MKey.Length);
            var written = Encoding.UTF8.GetChars(keyBytes, keyCharBuffer);
            var key = keyCharBuffer.Slice(0, written);

            switch (key)
            {
                case Assimp.MaterialNameBase:
                    material.EmbeddedName = MatUtils.ParsePropertyString(prop, charBuffer).ToString();
                    break;
                case Assimp.MatkeyTextureBase:
                    var texturePath = MatUtils.ParsePropertyString(prop, charBuffer);
                    var textureType = (TextureType)prop->MSemantic;
                    AttachTextureToMaterial(material, ctx.EmbeddedContext.Textures, texturePath, textureType);
                    break;
                case Assimp.MaterialOpacityBase:
                    var color = matParams.Color ?? Color4.White;
                    color.A = MatUtils.ParseFloatProp(prop);
                    matParams.Color = color;
                    break;
                case Assimp.MaterialShininessStrengthBase:
                    matParams.Shininess = MatUtils.ParseFloatProp(prop);
                    break;
                case Assimp.MatkeySpecularFactor:
                    var matSpecColor = matParams.SpecularColor ?? Color4.White;
                    matSpecColor.A = MatUtils.ParseFloatProp(prop);
                    matParams.SpecularColor = matSpecColor;
                    break;
                case Assimp.MaterialColorDiffuseBase:
                    matParams.Color = (Color4)MatUtils.ParseVectorProp(prop);
                    break;
                case Assimp.MaterialColorSpecularBase:
                    var specularColor = (Color4)MatUtils.ParseVectorProp(prop);
                    if(matParams.SpecularColor.HasValue) 
                        matParams.SpecularColor = specularColor with { A = matParams.SpecularColor.Value.A };
                    else
                        matParams.SpecularColor = specularColor;
                    break;
                case Assimp.MaterialTwosidedBase:
                    if(MatUtils.ParseIntProp(prop) == 1) matParams.DisableFlags |= GfxDrawFlags.Cull;
                    break;
                case Assimp.MaterialBlendFuncBase:
                    var blend = (Silk.NET.Assimp.BlendMode)MatUtils.ParseIntProp(prop);
                    if(blend == 0) break;
                    matParams.DrawFunctions = matParams.DrawFunctions.Patch(new GfxDrawFunctions(BlendMode.Additive));
                    matParams.EnableFlags |= GfxDrawFlags.Blend;
                    break;
                case "$mat.gltf.pbrMetallicRoughness.metallicFactor":
                    matParams.Metallic = MatUtils.ParseFloatProp(prop);
                    break;
                case "$mat.gltf.pbrMetallicRoughness.roughnessFactor":
                    matParams.Roughness = MatUtils.ParseFloatProp(prop);
                    break;

            }
        }

    }

    private static void AttachTextureToMaterial(
        EmbeddedSceneMaterial material,
        List<EmbeddedSceneTexture> textures,
        Span<char> texturePath,
        TextureType type)
    {
        if (!texturePath.StartsWith("*") || texturePath.Length < 2)
            throw new ArgumentException($"Invalid texture path {texturePath}", nameof(texturePath));

        var id = texturePath[1..];
        if (!int.TryParse(id, out var textureIndex) || (uint)textureIndex > textures.Count)
            throw new InvalidOperationException($"Invalid texture index {id}");

        var texture = textures[textureIndex];
        if (textures[textureIndex].TextureIndex != textureIndex)
        {
            throw new InvalidOperationException(
                $"Property texture index {textureIndex} does not match {texture.TextureIndex}");
        }

        if (material.Textures.Contains(texture.GId)) return;
        if (!MatUtils.ToSystemEnums(type, out var kind, out var format))
        {
            kind = TextureUsage.Albedo;
            format = TexturePixelFormat.SrgbAlpha;
        }
        
        texture.SlotKind = kind;
        texture.PixelFormat = format;
        material.Textures.Add(texture.GId);
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoadTextureData(EmbeddedImportContext context, AssimpTexture* aiTex, EmbeddedSceneTexture texture)
    {
        int width = (int)aiTex->MWidth, height = (int)aiTex->MHeight;
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
        var ptr = (byte*)aiTex->PcData;
        //texture.PixelDataBlock =
        context.RegisterTexture(texture, ptr, sizeInBytes);
        return sizeInBytes;
        //return TextureImporter.ImportUnmanagedTexture(ptr, sizeInBytes, width, height, format, out size);
    }

}

file static unsafe class MatUtils
{
    
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
            case TextureType.GltfMetallicRoughness:
                kind = TextureUsage.Roughness;
                format = TexturePixelFormat.Rgba;
                return  true;
            default:
                kind = 0;
                format = 0;
                return false;
        }
    }

    public static float ParseFloatProp(MaterialProperty* prop)
    {
        if (prop == null || prop->MData == null || prop->MDataLength < sizeof(float))
            Throwers.InvalidArgument(nameof(prop));

        return MemoryMarshal.Read<float>(new ReadOnlySpan<byte>(prop->MData, (int)prop->MDataLength));
    }
    
    public static int ParseIntProp(MaterialProperty* prop)
    {
        var length = (int)prop->MDataLength;
        if (prop == null || prop->MData == null || (length != 4 && length != 1))
            Throwers.InvalidArgument(nameof(prop));

        return length == 1 ? *prop->MData : MemoryMarshal.Read<int>(new ReadOnlySpan<byte>(prop->MData, sizeof(int)));
    }


    public static Vector4 ParseVectorProp(MaterialProperty* prop)
    {
        var length = (int)prop->MDataLength;
        if (prop == null || prop->MData == null || (length != 16 && length != 12))
            Throwers.InvalidArgument(nameof(prop));

        ref var b0 = ref Unsafe.AsRef<byte>(prop->MData);
        var span = MemoryMarshal.CreateSpan(ref b0, length);

        return length == 16 ? MemoryMarshal.Read<Vector4>(span) : MemoryMarshal.Read<Vector3>(span).AsVector4();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Span<char> ParsePropertyString(MaterialProperty* prop, Span<char> charBuffer)
    {
        if (prop->MType != PropertyTypeInfo.String)
            Throwers.InvalidArgument(nameof(prop));

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