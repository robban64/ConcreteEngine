using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Shaders;

internal record struct ShaderCreationInfo(ShaderId ShaderId, int Samplers);

internal readonly ref struct ShaderPayload(string vs, string fs, long vsSize, long fsSize)
{
    public readonly string Vs = vs;
    public readonly string Fs = fs;
    public readonly long VsSize = vsSize;
    public readonly long FsSize = fsSize;
}