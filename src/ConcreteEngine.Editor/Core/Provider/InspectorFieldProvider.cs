using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Core.Provider.Impl;

namespace ConcreteEngine.Editor.Core.Provider;

internal sealed class InspectorFieldProvider
{
    public static InspectorFieldProvider Instance = null!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Create()
    {
        if (Instance != null) throw new InvalidOperationException("Instance is not null");
        Instance = new InspectorFieldProvider();

        Instance.Allocate();
    }

    private void Allocate()
    {
        var allocator = TextBuffers.PersistentArena;
        //ModelInstanceFields.Allocate(allocator);
        ParticleInstanceFields.Allocate(allocator);
        MaterialFields.Allocate(allocator);
        TextureFields.Allocate(allocator);
        LightningFields.Allocate(allocator);
        PostFxFields.Allocate(allocator);
    }

    private InspectorFieldProvider() { }

    //public readonly InspectModelInstanceFields ModelInstanceFields = new();
    public readonly InspectParticleFields ParticleInstanceFields = new();

    public readonly InspectMaterialFields MaterialFields = new();
    public readonly InspectTextureFields TextureFields = new();

    public readonly InspectLightningFields LightningFields = new();
    public readonly InspectPostFxFields PostFxFields = new();
}