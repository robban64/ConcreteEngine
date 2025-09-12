
namespace ConcreteEngine.Core.Assets;

public interface IPendingResourceDescriptor
{
}

public sealed record PendingShaderDescriptor(string VertexSource, string FragmentSource) 
    : IPendingResourceDescriptor;
    
public sealed record PendingMeshDescriptor(string VertexSource, string FragmentSource) 
    : IPendingResourceDescriptor;
    
public sealed record PendingTextureDescriptor(string VertexSource, string FragmentSource) 
    : IPendingResourceDescriptor;