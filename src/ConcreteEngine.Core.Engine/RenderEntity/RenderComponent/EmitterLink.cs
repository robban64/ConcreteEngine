using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Graphics.Particles;

namespace ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct EmitterLink(Id16<ParticleEmitter> emitterId) : IRenderComponent<EmitterLink>
{
    public Id16<ParticleEmitter> EmitterId = emitterId;
}