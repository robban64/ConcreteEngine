#region

using System.Numerics;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Utility;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Renderer.State;

public sealed class RenderCamera
{
    public RenderViewSnapshot RenderView;
    public LightView LightSpace;
    
    public bool UseLightViewOverride { get; internal set; }
    
    public void RestoreView() => UseLightViewOverride = false;
    public void ToggleLightView() => UseLightViewOverride = true;

}