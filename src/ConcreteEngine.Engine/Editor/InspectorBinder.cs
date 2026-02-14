using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Engine.Assets;

namespace ConcreteEngine.Engine.Editor;

internal static class InspectorBinder
{
    internal static void RegisterTypes()
    {
        InspectorRegistry.RegisterType(typeof(Model));
        InspectorRegistry.RegisterType(typeof(MeshEntry));
        InspectorRegistry.RegisterType(typeof(ModelAnimation));
        InspectorRegistry.RegisterType(typeof(AnimationClip));
        InspectorRegistry.RegisterType(typeof(MeshInfo));
        InspectorRegistry.RegisterType(typeof(ModelInfo));

    }
}