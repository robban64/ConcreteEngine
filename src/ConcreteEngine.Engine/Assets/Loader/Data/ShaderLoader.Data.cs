using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader.Data;

internal readonly struct ShaderCreationInfo(ShaderId shaderId, int samplers)
{
    public readonly ShaderId ShaderId = shaderId;
    public readonly int Samplers = samplers;
}

internal readonly struct ShaderPayload(NativeViewPtr<byte> vs, NativeViewPtr<byte> fs, long vsSize, long fsSize)
{
    public readonly NativeViewPtr<byte> Vs = vs;
    public readonly NativeViewPtr<byte> Fs = fs;
    public readonly long VsSize = vsSize;
    public readonly long FsSize = fsSize;
}