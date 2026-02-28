using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader.Data;

internal readonly struct ShaderCreationInfo(ShaderId shaderId, int samplers)
{
    public readonly ShaderId ShaderId = shaderId;
    public readonly int Samplers = samplers;
}

internal readonly ref struct ShaderPayload(string vs, string fs, long vsSize, long fsSize)
{
    public readonly string Vs = vs;
    public readonly string Fs = fs;
    public readonly long VsSize = vsSize;
    public readonly long FsSize = fsSize;
}