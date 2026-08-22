using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;

[Inspect]
public sealed class EnvironmentSettings
{
    [InspectInclude]
    public AmbientSettings Ambient { get; } = new ();

    [InspectInclude]
    public FogSettings FogSettings { get; } = new();
    
    public bool Commit()
    {
        var anyDirty = false;
        anyDirty |= Ambient.Commit();
        anyDirty |= FogSettings.Commit();
        return anyDirty;
    }
}