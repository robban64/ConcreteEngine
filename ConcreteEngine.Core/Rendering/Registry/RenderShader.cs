#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

public sealed class RenderShader : IComparable<ShaderId>
{
    public ShaderId Id { get; }
    public int SamplerSlots { get; }

    internal RenderShader(ShaderId id, ShaderMeta meta)
    {
        Id = id;
        SamplerSlots = meta.SamplerSlots;
    }

    public Dictionary<string, int> CreateNewUniformDict(GfxShaders gfx)
    {
        var uniformPairs = gfx.GetUniformList(Id);
        var uniforms = new Dictionary<string, int>(uniformPairs.Count);
        
        foreach (var (uniform, location) in uniformPairs)
        {
            uniforms.Add(uniform, location);
        }
        return uniforms;
    }
    
    public int CompareTo(ShaderId other) => Id.Value.CompareTo(other.Value);
}