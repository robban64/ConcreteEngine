using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics;

public interface IGpuUploadSink
{
    IMeshFactory MeshFactory { get; }

    FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta);
    ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta);
    TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta);
    TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta);
}


internal sealed class GpuUploadSink : IGpuUploadSink
{
    private readonly IResourceAllocator _allocator;

    public GpuUploadSink(IResourceAllocator allocator)
    {
        _allocator = allocator;
    }

    public FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta)
    {
        return _allocator.CreateFramebuffer(in desc, out meta);
    }

    public ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta)
    {
        return _allocator.CreateShader(vertexSource, fragmentSource, out meta);
    }

    public TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta)
    {
        return _allocator.CreateTexture2D(data, in desc, out meta);
    }

    public TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta)
    {
        return _allocator.CreateCubeMap(data, in desc, out meta);
    }
}