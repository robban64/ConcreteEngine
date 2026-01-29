using System.Buffers.Text;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.Assimp;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpMaterial = Silk.NET.Assimp.Material;
using AssimpTexture = Silk.NET.Assimp.Texture;

namespace ConcreteEngine.Engine.Assets.Loader.ModelImporterV2;

internal sealed unsafe partial class ModelImporter
{
    public static bool ProcessProperties(AssimpMaterial* material)
    {
        Span<char> charBuffer = stackalloc char[256];
        Span<char> keyCharBuffer = stackalloc char[64];

        for (var i = 0; i < material->MNumProperties; i++)
        {
            var prop = material->MProperties[i];
            
            var keyBytes = new Span<byte>(prop->MKey.Data, (int)prop->MKey.Length);
            var written = Encoding.UTF8.GetChars(keyBytes, keyCharBuffer);
            var key = keyCharBuffer.Slice(0, written);
            
            MaterialParams matData = default;

            switch (key)
            {
                case "?mat.name":
                    var materialName = MatUtils.ParsePropertyString(prop, charBuffer);
                    break;
                case "$tex.file":
                    var texturePath = MatUtils.ParsePropertyString(prop, charBuffer);
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
                    MatUtils.ParseVectorProp(prop, out var diffuse);
                    matData.Color = matData.Color with { R = diffuse.X, G = diffuse.Y, B = diffuse.Z };
                    break;
            }
        }

        return false;
    }
}

file static unsafe class MatUtils
{
    public static bool GetTextureProperties(TextureType type, out TextureUsage kind, out TexturePixelFormat format)
    {
        switch (type)
        {
            case TextureType.Diffuse:
                kind = TextureUsage.Albedo;
                format = TexturePixelFormat.SrgbAlpha;
                break;
            case TextureType.Normals:
                kind = TextureUsage.Normal;
                format = TexturePixelFormat.Rgba;
                break;
            case TextureType.Opacity:
                kind = TextureUsage.Mask;
                format = TexturePixelFormat.Red;
                break;
            default:
                kind = 0;
                format = 0;
                return false;
        }

        return true;
    }

    public static void ParseFloatProp(MaterialProperty* prop, out float value)
    {
        value = 0f;
        if (prop == null || prop->MData == null || prop->MDataLength < sizeof(float))
            return;

        value = MemoryMarshal.Read<float>(new ReadOnlySpan<byte>(prop->MData, (int)prop->MDataLength));
    }

    public static void ParseVectorProp(MaterialProperty* prop, out Vector4 data)
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

    public static Span<char> ParsePropertyString(MaterialProperty* prop, Span<char> charBuffer)
    {
        if (prop->MType != PropertyTypeInfo.String) return Span<char>.Empty;

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