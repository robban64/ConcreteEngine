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
    private readonly IGraphicsDevice _graphics;
    
    public IMeshFactory MeshFactory => _graphics.MeshFactory;

    public GpuUploadSink(IGraphicsDevice graphics)
    {
        _graphics = graphics;
    }

    public FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta)
    {
        return _graphics.CreateFramebuffer(in desc, out meta);
    }

    public ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta)
    {
        return _graphics.CreateShader(vertexSource, fragmentSource, out meta);
    }

    public TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta)
    {
        return _graphics.CreateTexture2D(data, in desc, out meta);
    }

    public TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta)
    {
        return _graphics.CreateCubeMap(data, in desc, out meta);
    }
}