using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Worlds.Mesh;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(ShortHandle<ParticleEmitter> emitter, MaterialId material)
    : IRenderComponent<ParticleComponent>
{
    public ShortHandle<ParticleEmitter> Emitter = emitter;
    public MaterialId Material = material;

    public static BoundingBox DefaultParticleBounds => new(new Vector3(-0.5f), new Vector3(0.5f));
}