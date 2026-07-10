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
        MaterialFields.Allocate(allocator);
        TextureFields.Allocate(allocator);
    }

    private InspectorFieldProvider() { }


    public readonly InspectMaterialFields MaterialFields = new();
    public readonly InspectTextureFields TextureFields = new();
}