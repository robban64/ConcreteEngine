using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Data;

internal readonly struct TexturePtrHandle(ImTextureRefPtr texturePtr, NativeHandle<TextureMeta> handle)
{
    public readonly ImTextureRefPtr TexturePtr = texturePtr;
    public readonly NativeHandle<TextureMeta> Handle = handle;

    public readonly bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TexturePtr.IsNull || Handle == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator ImTextureRef(TexturePtrHandle it) => *it.TexturePtr.Handle;

    public static TexturePtrHandle Null => new(ImTextureRefPtr.Null, default);
}