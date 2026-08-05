using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Data;

internal struct TexturePtrHandle(ImTextureRefPtr texturePtr, NativeHandle<TextureMeta> handle)
{
    public ImTextureRefPtr TexturePtr = texturePtr;
    public NativeHandle<TextureMeta> Handle = handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator ImTextureRef(TexturePtrHandle it) => *it.TexturePtr.Handle;

    public static TexturePtrHandle Null => new(ImTextureRefPtr.Null, default);
}