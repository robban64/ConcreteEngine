using System.Runtime.InteropServices;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(int emitter, MaterialId material) : IRenderComponent<ParticleComponent>
{
    public int Emitter = emitter;
    public MaterialId Material = material;

}