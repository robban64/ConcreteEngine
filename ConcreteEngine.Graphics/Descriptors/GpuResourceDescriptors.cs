using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Descriptors;

public record struct GpuShaderData(string VertexSource, string FragmentSource);

public readonly ref struct GpuTextureData(ReadOnlySpan<byte> pixelData, uint width, uint height)
{
    public readonly uint Width = width;
    public readonly uint Height = height;
    public readonly ReadOnlySpan<byte> PixelData = pixelData;
}

public record struct GpuTextureDescriptor(
    uint Width,
    uint Height,
    TexturePreset Preset,
    TextureKind Kind,
    EnginePixelFormat Format = EnginePixelFormat.Rgba,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Default,
    float LodBias = 0
);

public readonly ref struct GpuCubeMapData(
    ReadOnlySpan<byte> faceData1,
    ReadOnlySpan<byte> faceData2,
    ReadOnlySpan<byte> faceData3,
    ReadOnlySpan<byte> faceData4,
    ReadOnlySpan<byte> faceData5,
    ReadOnlySpan<byte> faceData6)
{
    public readonly ReadOnlySpan<byte> FaceData1 = faceData1;
    public readonly ReadOnlySpan<byte> FaceData2 = faceData2;
    public readonly ReadOnlySpan<byte> FaceData3 = faceData3;
    public readonly ReadOnlySpan<byte> FaceData4 = faceData4;
    public readonly ReadOnlySpan<byte> FaceData5 = faceData5;
    public readonly ReadOnlySpan<byte> FaceData6 = faceData6;
}

public record struct GpuCubeMapDescriptor(
    int Width,
    int Height,
    EnginePixelFormat Format
);