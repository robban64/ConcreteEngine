namespace ConcreteEngine.Renderer.Buffer;

public sealed class RenderUploadBuffers
{
    public readonly DrawCommandBuffer CommandBuffer = new();
    public readonly MaterialBuffer MaterialBuffer = new();
    public readonly SkinningBuffer SkinningBuffer = new();
}