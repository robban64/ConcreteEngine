using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Editor.Data;



public struct EditorVisualState
{
    public SunLightParams SunLight;
    public AmbientParams Ambient;
    public FogParams Fog;
    public ShadowParams Shadow;
    public PostEffectParams PostEffect;
}
