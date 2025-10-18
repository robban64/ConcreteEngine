using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderBuilderContext
{

    public GfxContext Gfx { get; private set; }
    public Size2D OutputSize { get; }
    public bool Done { get; internal set; }

    internal RenderPipelineVersion Version { get; set; } = RenderPipelineVersion.None;

    //public Action<RenderRegistryBuilder>? RegistrySetup { get; set; }
    internal Action<GfxContext, BatcherRegistry>? BatcherSetup { get; set; }
    internal Action<IDrawCommandCollector>? CollectorSetup { get; set; }

    internal Action<IRenderFboRegistry>? FboSetup { get; set; }
   // public Action<IRenderShaderRegistry>? ShaderSetup { get; set; }

   //internal Action<Span<ShaderId>>? ShaderProvider { get; set; }
   internal Func<List<ShaderId>>? ShaderProvider { get; set; }

   internal Func<RenderCoreShaders>? CoreShaderSetup { get; set; }

    internal RenderBuilderContext(GfxContext gfx, Size2D outputSize)
    {
        Gfx = gfx;
        OutputSize = outputSize;
    }

    
    internal void Reset()
    {
        Gfx = null!;
        //RegistrySetup = null;
        BatcherSetup = null;
        CollectorSetup = null;
        FboSetup = null;
        ShaderProvider = null;
        CoreShaderSetup = null;
    }
}