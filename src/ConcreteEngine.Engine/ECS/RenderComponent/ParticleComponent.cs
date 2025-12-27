using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Identity;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Mesh;
using ConcreteEngine.Renderer;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(Handle<ParticleEmitter> emitter, MaterialId material) : IRenderComponent<ParticleComponent>
{
    public Handle<ParticleEmitter> Emitter = emitter;
    public MaterialId Material = material;

    public static BoundingBox DefaultParticleBounds => new(new Vector3(-0.5f), new Vector3(0.5f));
}