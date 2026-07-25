using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Handles;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Core.Data;

internal struct TexturePtrHandle(ImTextureRefPtr texturePtr, GfxHandle handle)
{
    public ImTextureRefPtr TexturePtr = texturePtr;
    public GfxHandle Handle = handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe implicit operator ImTextureRef(TexturePtrHandle it) => *it.TexturePtr.Handle;

    public static TexturePtrHandle Null => new(ImTextureRefPtr.Null, default);
}