using ConcreteEngine.Graphics.Descriptors;

namespace ConcreteEngine.Graphics.Resources;


public interface IGpuResourcePayload;

public interface IGpuResourcePayloadProvider<out TPayload, TResult> where TPayload : class where TResult : struct
{
    IReadOnlyList<TPayload> Get();
    void Callback(ReadOnlySpan<TResult> result);
}

public interface IGpuLazyResourcePayloadProvider<TPayload, TResult> where TPayload : class where TResult : struct
{
    bool TryGet(out int queueIndex, out TPayload payload);
    void Callback(int queueIndex, in TResult result);
}

public interface IGpuShaderPayloadProvider : IGpuResourcePayloadProvider<GpuShaderPayload, (ShaderId, ShaderMeta)>;

public interface IGpuLazyTexturePayloadProvider
    : IGpuLazyResourcePayloadProvider<GpuTexturePayload, (TextureId, TextureMeta)>;

public interface IGpuLazyCubeMapPayloadProvider
    : IGpuLazyResourcePayloadProvider<GpuCubeMapPayload, (TextureId, TextureMeta)>;

public interface IGpuLazyMeshPayloadProvider
    : IGpuLazyResourcePayloadProvider<GpuMeshPayload, (MeshId, MeshMeta)>;
