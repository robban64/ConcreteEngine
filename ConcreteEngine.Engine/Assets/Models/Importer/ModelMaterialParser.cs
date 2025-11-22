using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
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

internal  class ModelMaterialEmbeddedEntry
{
    public string? Name { get; set; }
    public Color4 Color { get; set; }

    public Dictionary<string, ModelEmbeddedTextureEntry> EmbeddedTextures { get; } = new();
}

internal sealed  class ModelEmbeddedTextureEntry
{
    public required string Name { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required TextureSlotKind SlotKind { get; init; }
    public required TexturePixelFormat PixelFormat { get; init; }
    public required byte[] PixelData { get; set; } = Array.Empty<byte>();
}

internal sealed class ModelMaterialParser
{
    private bool isActive = false;


    internal unsafe ModelMaterialEmbeddedEntry[] ProcessSceneMaterials(AssimpScene* scene)
    {
        InvalidOpThrower.ThrowIf(isActive, nameof(isActive));
        isActive = true;

        var entries = new ModelMaterialEmbeddedEntry[scene->MNumMaterials];
        for (int i = 0; i < scene->MNumMaterials; i++)
        {
            var aMaterial = scene->MMaterials[i];
            entries[i] = new ModelMaterialEmbeddedEntry();
            ProcessMaterial(scene, aMaterial, entries[i]);
        }

        isActive = false;
        return entries;
    }


    private unsafe void ProcessMaterial(AssimpScene* scene, AssimpMaterial* material, ModelMaterialEmbeddedEntry entry)
    {
        for (int i = 0; i < material->MNumProperties; i++)
        {
            var prop = material->MProperties[i];
            var key = prop->MKey.AsString;

            if (key == "?mat.name")
            {
                ProcessName(prop, entry);
            }
            else if (key == "$tex.file")
            {
                ProcessTexture(scene, prop, entry);
            }
            else if (key == "$clr.diffuse")
            {
                ProcessParams(prop, entry);
            }
        }
    }

    private unsafe void ProcessName(MaterialProperty* prop, ModelMaterialEmbeddedEntry entry)
    {
        if (entry.Name != null) throw new ArgumentException(nameof(entry.Name));

        var matName = ParsePropertyString(prop);
        entry.Name = matName;
    }

    private unsafe void ProcessTexture(AssimpScene* scene, MaterialProperty* prop, ModelMaterialEmbeddedEntry entry)
    {
        if (prop->MIndex != 0) return;

        var type = (TextureType)prop->MSemantic;
        var texturePath = ParsePropertyString(prop);
        if (string.IsNullOrWhiteSpace(texturePath)) return;

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
            default: return;
        }

        var texture = scene->MTextures[textureIndex];
        LoadTextureData(texture, entry, kind, format);
    }


    private unsafe void LoadTextureData(AssimpTexture* texture, ModelMaterialEmbeddedEntry entry, TextureSlotKind kind,
        TexturePixelFormat format)
    {
        string textureName = texture->MFilename.AsString;
        if (string.IsNullOrEmpty(textureName))
        {
            Console.WriteLine("Invalid texture name");
            return;
        }

        if (entry.EmbeddedTextures.ContainsKey(textureName))
        {
            throw new ArgumentException($"Duplicated texture names {textureName}",
                nameof(entry.EmbeddedTextures));
        }

        int width = (int)texture->MWidth, height = (int)texture->MHeight;

        var length = width * height * 4;
        var ptr = (byte*)texture->PcData;
        var span = new ReadOnlySpan<byte>(ptr, length);

        byte[] buffer = new byte[length];
        span.CopyTo(buffer);

        var textureEntry = new ModelEmbeddedTextureEntry
        {
            Name = textureName,
            Width = width,
            Height = height,
            PixelFormat = format,
            SlotKind = kind,
            PixelData = buffer
        };

        entry.EmbeddedTextures.Add(textureName, textureEntry);
    }


    private unsafe void ProcessParams(MaterialProperty* prop, ModelMaterialEmbeddedEntry entry)
    {
        var length = (int)prop->MDataLength;
        if (length == 0) return;

        InvalidOpThrower.ThrowIf(length != 16 && length != 12, nameof(length));

        ref var b0 = ref Unsafe.AsRef<byte>(prop->MData);
        var span = MemoryMarshal.CreateSpan(ref b0, (int)prop->MDataLength);

        if (length == 16)
        {
            var res = MemoryMarshal.Read<Vector4>(span);
            entry.Color = Color4.FromVector4(in res);
        }
        else if (length == 12)
        {
            var res = MemoryMarshal.Read<Vector3>(span);
            entry.Color = Color4.FromVector3(in res);
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