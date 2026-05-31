using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(Id16<ParticleEmitter> emitter, MaterialId material)
    : IRenderComponent<ParticleComponent>
{
    public Id16<ParticleEmitter> Emitter = emitter;
    public MaterialId Material = material;
}