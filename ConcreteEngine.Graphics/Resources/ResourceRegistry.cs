namespace ConcreteEngine.Graphics.Resources;

internal interface IResourceRegistry
{
    public ShaderRegistry ShaderRegistry { get; }
    public MeshRegistry MeshRegistry { get; }

    public FrameBufferRegistry FboRegistry { get; }
}

internal sealed class ResourceRegistry : IResourceRegistry
{
    public ResourceRegistry(ShaderRegistry shaderRegistry, MeshRegistry meshRegistry, FrameBufferRegistry fboRegistry)
    {
        ShaderRegistry = shaderRegistry;
        MeshRegistry = meshRegistry;
        FboRegistry = fboRegistry;
    }

    public ShaderRegistry ShaderRegistry { get; }
    public MeshRegistry MeshRegistry { get; }
    public FrameBufferRegistry FboRegistry { get; }
}