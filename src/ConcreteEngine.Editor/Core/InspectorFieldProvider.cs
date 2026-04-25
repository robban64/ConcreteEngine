using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Inspector.Impl;

namespace ConcreteEngine.Editor.Core;

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
        SceneFields.Allocate(allocator);
        ModelInstanceFields.Allocate(allocator);
        ParticleInstanceFields.Allocate(allocator);
        MaterialFields.Allocate(allocator);
        TextureFields.Allocate(allocator);
        CameraFields.Allocate(allocator);
        LightningFields.Allocate(allocator);
        PostFxFields.Allocate(allocator);
    }

    private InspectorFieldProvider() { }

    public readonly InspectSceneFields SceneFields = new();
    public readonly InspectModelInstanceFields ModelInstanceFields = new();
    public readonly InspectParticleFields ParticleInstanceFields = new();

    public readonly InspectMaterialFields MaterialFields = new();
    public readonly InspectTextureFields TextureFields = new();

    public readonly InspectCameraFields CameraFields = new();
    public readonly InspectLightningFields LightningFields = new();
    public readonly InspectPostFxFields PostFxFields = new();
}
