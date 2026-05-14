using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(Id32<ParticleEmitter> emitter, MaterialId material) : IRenderComponent<ParticleComponent>
{
    public Id32<ParticleEmitter> Emitter = emitter;
    public MaterialId Material = material;
}