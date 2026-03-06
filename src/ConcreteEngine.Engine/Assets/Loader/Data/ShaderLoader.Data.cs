using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader.Data;

internal readonly struct ShaderCreationInfo(ShaderId shaderId, int samplers)
{
    public readonly ShaderId ShaderId = shaderId;
    public readonly int Samplers = samplers;
}

internal readonly ref struct ShaderPayload(ReadOnlySpan<byte> vs, ReadOnlySpan<byte> fs, long vsSize, long fsSize)
{
    public readonly ReadOnlySpan<byte> Vs = vs;
    public readonly ReadOnlySpan<byte> Fs = fs;
    public readonly long VsSize = vsSize;
    public readonly long FsSize = fsSize;
}