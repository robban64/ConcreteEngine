using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;
using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpMaterial = Silk.NET.Assimp.Material;
using AssimpTexture = Silk.NET.Assimp.Texture;

namespace ConcreteEngine.Engine.Assets.Models.Loader.AssimpImporter;

internal sealed class AssimpMaterialProcessor(ModelLoaderState state)
{
    private bool isActive = false;

    private readonly HashSet<string> _processedTextureNames = new(4);

    internal unsafe void ProcessSceneMaterials(AssimpScene* scene)
    {
        InvalidOpThrower.ThrowIf(isActive, nameof(isActive));
        _processedTextureNames.Clear();
        isActive = true;

        for (var i = 0; i < scene->MNumMaterials; i++)
        {
            var aMaterial = scene->MMaterials[i];
            var mat = new MaterialEmbeddedDescriptor { GId = Guid.NewGuid(), MaterialIndex = i };

            if (!ProcessMaterial(scene, aMaterial, mat)) continue;

            if (string.IsNullOrWhiteSpace(mat.EmbeddedName))
                throw new InvalidOperationException(nameof(mat.EmbeddedName));

            var assetName = state.ToEmbeddedAssetName("Materials", i);
            mat.AssetName = assetName;
            mat.IsAnimated = state.IsAnimated;
            mat.FileSpec =
            [
                new AssetFileSpec(AssetStorageKind.Embedded, assetName, mat.EmbeddedName, 0,
                    source: state.Filename)
            ];

            state.AppendMaterial(mat);
        }

        isActive = false;
    }


    private unsafe bool ProcessMaterial(AssimpScene* scene, AssimpMaterial* material,
        MaterialEmbeddedDescriptor descriptor)
    {
        var hasName = false;
        var hasTexture = false;

        ref var resultParams = ref descriptor.Params;
        for (var i = 0; i < material->MNumProperties; i++)
        {
            var prop = material->MProperties[i];
            var key = prop->MKey.AsString;
            switch (key)
            {
                case "?mat.name":
                    if (ProcessName(prop, descriptor)) hasName = true;
                    break;
                case "$tex.file":
                    if (ProcessTexture(scene, prop, descriptor)) hasTexture = true;
                    break;
                case "$mat.opacity":
                    ProcessFloatParam(prop, out resultParams.Opacity);
                    resultParams.HasOpacity = true;
                    break;
                case "$mat.shininess":
                    ProcessFloatParam(prop, out resultParams.Shininess);
                    resultParams.HasShininess = true;
                    break;
                case "$mat.shinpercent":
                    ProcessFloatParam(prop, out resultParams.SpecularFactor);
                    resultParams.HasSpecularFactor = true;
                    break;
                case "$clr.specular":
                    ProcessVec3Or4Param(prop, out resultParams.Specular);
                    resultParams.HasSpecular = true;
                    break;
                case "$clr.diffuse":
                    ProcessVec3Or4Param(prop, out var diffuse);
                    resultParams.Color = Color4.FromVector4(in diffuse);
                    resultParams.HasColor = true;
                    break;
                //case "$clr.emissive":  ProcessParams(prop, out descriptor.EmissiveColor); break;
                //case "$clr.ambient":   ProcessParams(prop, out descriptor.AmbientColor); break;
            }
        }

        return hasTexture && hasName;
    }

    private unsafe bool ProcessName(MaterialProperty* prop, MaterialEmbeddedDescriptor descriptor)
    {
        if (descriptor.EmbeddedName != null) throw new ArgumentException(nameof(descriptor.EmbeddedName));

        var matName = ParsePropertyString(prop);
        if (string.IsNullOrWhiteSpace(matName)) return false;
        descriptor.EmbeddedName = matName;

        return true;
    }

    private unsafe bool ProcessTexture(AssimpScene* scene, MaterialProperty* prop,
        MaterialEmbeddedDescriptor descriptor)
    {
        if (prop->MIndex != 0) return false;

        var type = (TextureType)prop->MSemantic;
        var texturePath = ParsePropertyString(prop);
        if (string.IsNullOrWhiteSpace(texturePath)) return false;

        if (!texturePath.StartsWith("*") || texturePath.Length < 2)
            throw new InvalidOperationException($"Invalid texture path {texturePath}");

        var id = texturePath[1..];
        if (!int.TryParse(id, out var textureIndex))
            throw new InvalidOperationException($"Invalid texture index {id}");


        TextureSlotKind kind;
        TexturePixelFormat format;
        switch (type)
        {
            case TextureType.Diffuse:
                kind = TextureSlotKind.Albedo;
                format = TexturePixelFormat.SrgbAlpha;
                break;
            case TextureType.Normals:
                kind = TextureSlotKind.Normal;
                format = TexturePixelFormat.Rgba;
                break;
            case TextureType.Opacity:
                kind = TextureSlotKind.Mask;
                format = TexturePixelFormat.Red;
                break;
            default: return false;
        }

        var texture = scene->MTextures[textureIndex];
        return LoadTextureData(texture, descriptor, textureIndex, kind, format);
    }


    private unsafe bool LoadTextureData(
        AssimpTexture* texture,
        MaterialEmbeddedDescriptor descriptor,
        int textureIndex,
        TextureSlotKind kind,
        TexturePixelFormat format)
    {
        var textureName = texture->MFilename.AsString;
        //if (string.IsNullOrEmpty(textureName)) textureName = $"texture{textureIndex}";

        if (descriptor.EmbeddedTextures.ContainsKey((descriptor.MaterialIndex, textureIndex)))
        {
            throw new ArgumentException($"Duplicated texture names {textureName}",
                nameof(descriptor.EmbeddedTextures));
        }


        int width = (int)texture->MWidth, height = (int)texture->MHeight;
        var length = 0;
        // Compressed mode (PNG, JPG)
        // width = file size in bytes
        if (height == 0)
        {
            length = width;
        }
        // raw mode (BGRA8888)
        // standard: width height and 4 bytes per pixel
        else
        {
            length = width * height * 4;
        }

        InvalidOpThrower.ThrowIf(length < 4, nameof(length));

        var ptr = (byte*)texture->PcData;
        var span = new ReadOnlySpan<byte>(ptr, length);

        var buffer = new byte[length];
        span.CopyTo(buffer);

        var assetName = state.ToEmbeddedAssetName("Textures", textureIndex);
        if (textureName.Length > 0)
            _processedTextureNames.Add(textureName);
        var textureEntry = new TextureEmbeddedDescriptor
        {
            GId = Guid.NewGuid(),
            AssetName = assetName,
            EmbeddedName = textureName,
            Width = width,
            Height = height,
            PixelFormat = format,
            SlotKind = kind,
            PixelData = buffer,
            Index = textureIndex,
            FileSpec =
            [
                new AssetFileSpec(AssetStorageKind.Embedded, assetName, textureName, buffer.Length,
                    source: state.Filename)
            ]
        };
        state.AppendTexture(textureEntry);
        descriptor.EmbeddedTextures.Add((descriptor.MaterialIndex, textureIndex), textureEntry.GId);

        return true;
    }

    private static unsafe void ProcessFloatParam(MaterialProperty* prop, out float value)
    {
        value = 0f;
        if (prop == null || prop->MData == null || prop->MDataLength < sizeof(float))
            return;

        value = MemoryMarshal.Read<float>(new ReadOnlySpan<byte>(prop->MData, (int)prop->MDataLength));
    }

    private static unsafe void ProcessVec3Or4Param(MaterialProperty* prop, out Vector4 data)
    {
        data = default;

        var length = (int)prop->MDataLength;
        if (length == 0) return;

        InvalidOpThrower.ThrowIf(length != 16 && length != 12, nameof(length));

        ref var b0 = ref Unsafe.AsRef<byte>(prop->MData);
        var span = MemoryMarshal.CreateSpan(ref b0, (int)prop->MDataLength);

        if (length == 16)
        {
            data = MemoryMarshal.Read<Vector4>(span);
        }
        else if (length == 12)
        {
            var res = MemoryMarshal.Read<Vector3>(span);
            data = res.AsVector4();
        }
    }

    private static unsafe string ParsePropertyString(MaterialProperty* prop)
    {
        if (prop->MType != PropertyTypeInfo.String) return "";

        // 4-byte integer followed by the string
        var length = *(int*)prop->MData;
        var stringStart = prop->MData + 4;

        if (length > 0 && length < prop->MDataLength)
        {
            return Encoding.UTF8.GetString(stringStart, length);
        }

        return "";
    }
}