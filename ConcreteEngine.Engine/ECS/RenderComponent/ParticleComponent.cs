using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(int emitterHandle, MaterialId material) : IRenderComponent<ParticleComponent>
{
    public int EmitterHandle = emitterHandle;
    public MaterialId Material = material;

    public static BoundingBox DefaultParticleBounds => new(new Vector3(-0.5f), new Vector3(0.5f));
}