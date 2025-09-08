using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Descriptors;

public sealed record GpuResourcePayloadCollection
{
    public required IGpuShaderPayloadProvider Shaders { get; init; }
    public required IGpuLazyMeshPayloadProvider Meshes { get; init; }
    public required IGpuLazyTexturePayloadProvider Textures { get; init; }
    public required IGpuLazyCubeMapPayloadProvider CubeMaps { get; init; }
}

public sealed record GpuShaderPayload(string VertexSource, string FragmentSource) : IGpuResourcePayload;

public sealed record GpuMeshPayload(
    GpuMeshData<Vertex3D, uint> GpuMeshData,
    GpuMeshDescriptor Descriptor)
    : IGpuResourcePayload;

public sealed record GpuTexturePayload(
    ReadOnlyMemory<byte> PixelData,
    int Width,
    int Height,
    EnginePixelFormat Format,
    TexturePreset Preset,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Default,
    float LodBias = 0,
    bool NullPtrData = false
) : IGpuResourcePayload;

public sealed record GpuCubeMapPayload(
    ReadOnlyMemory<byte>[] FaceData,
    int Width,
    int Height,
    EnginePixelFormat Format
) : IGpuResourcePayload;
