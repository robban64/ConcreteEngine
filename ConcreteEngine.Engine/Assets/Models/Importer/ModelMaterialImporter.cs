using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;
using Silk.NET.Assimp;
using StbImageSharp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMaterial = Silk.NET.Assimp.Material;
using AssimpTexture = Silk.NET.Assimp.Texture;

namespace ConcreteEngine.Engine.Assets.Models.Importer;

internal sealed class ModelMaterialImporter
{
    private bool isActive = false;

    internal unsafe ModelMaterialEmbeddedDescriptor[] ProcessSceneMaterials(AssimpScene* scene)
    {
        InvalidOpThrower.ThrowIf(isActive, nameof(isActive));
        isActive = true;

        var entries = new List<ModelMaterialEmbeddedDescriptor>();
        for (int i = 0; i < scene->MNumMaterials; i++)
        {
            var aMaterial = scene->MMaterials[i];
            var entry = new ModelMaterialEmbeddedDescriptor();
            if (ProcessMaterial(scene, aMaterial, entry)) 
                entries.Add(entry);
        }

        isActive = false;
        return entries.ToArray();
    }

    private static unsafe bool ProcessMaterial(AssimpScene* scene, AssimpMaterial* material,
        ModelMaterialEmbeddedDescriptor descriptor)
    {
        bool hasName = false;
        bool hasTexture = false;
        for (int i = 0; i < material->MNumProperties; i++)
        {
            var prop = material->MProperties[i];
            var key = prop->MKey.AsString;

            if (key == "?mat.name")
            {
                if (ProcessName(prop, descriptor)) hasName = true;
            }
            else if (key == "$tex.file")
            {
                if (ProcessTexture(scene, prop, descriptor)) hasTexture = true;
            }
            else if (key == "$clr.diffuse")
            {
                ProcessParams(prop, descriptor);
            }
        }

        return hasTexture && hasName;
    }

    private static unsafe bool ProcessName(MaterialProperty* prop, ModelMaterialEmbeddedDescriptor descriptor)
    {
        if (descriptor.Name != null) throw new ArgumentException(nameof(descriptor.Name));

        var matName = ParsePropertyString(prop);
        if (string.IsNullOrWhiteSpace(matName)) return false;
        descriptor.Name = matName;

        return true;
    }

    private static unsafe bool ProcessTexture(AssimpScene* scene, MaterialProperty* prop,
        ModelMaterialEmbeddedDescriptor descriptor)
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
            default: return false;
        }

        var texture = scene->MTextures[textureIndex];
        return LoadTextureData(texture, descriptor, kind, format);
    }


    private static unsafe bool LoadTextureData(AssimpTexture* texture, ModelMaterialEmbeddedDescriptor descriptor,
        TextureSlotKind kind,
        TexturePixelFormat format)
    {
        string textureName = texture->MFilename.AsString;
        if (string.IsNullOrEmpty(textureName))
        {
            Console.WriteLine("Invalid texture name");
            return false;
        }

        if (descriptor.EmbeddedTextures.ContainsKey(textureName))
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

        byte[] buffer = new byte[length];
        span.CopyTo(buffer);

        var textureEntry = new TextureEmbeddedDescriptor
        {
            Name = textureName,
            Width = width,
            Height = height,
            PixelFormat = format,
            SlotKind = kind,
            PixelData = buffer,
        };

        descriptor.EmbeddedTextures.Add(textureName, textureEntry);

        return true;
    }


    private static unsafe void ProcessParams(MaterialProperty* prop, ModelMaterialEmbeddedDescriptor descriptor)
    {
        var length = (int)prop->MDataLength;
        if (length == 0) return;

        InvalidOpThrower.ThrowIf(length != 16 && length != 12, nameof(length));

        ref var b0 = ref Unsafe.AsRef<byte>(prop->MData);
        var span = MemoryMarshal.CreateSpan(ref b0, (int)prop->MDataLength);

        if (length == 16)
        {
            var res = MemoryMarshal.Read<Vector4>(span);
            descriptor.Color = Color4.FromVector4(in res);
        }
        else if (length == 12)
        {
            var res = MemoryMarshal.Read<Vector3>(span);
            descriptor.Color = Color4.FromVector3(in res);
        }
    }

    private static unsafe string ParsePropertyString(MaterialProperty* prop)
    {
        if (prop->MType != PropertyTypeInfo.String) return "";

        // 4-byte integer followed by the string
        int length = *(int*)prop->MData;
        byte* stringStart = prop->MData + 4;

        if (length > 0 && length < prop->MDataLength)
        {
            return System.Text.Encoding.UTF8.GetString(stringStart, length);
        }

        return "";
    }
}