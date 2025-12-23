using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Shaders;

internal record struct ShaderCreationInfo(ShaderId ShaderId, int Samplers);

internal readonly ref struct ShaderPayload(string vs, string fs, AssetFileSpec vertSpec, AssetFileSpec fragSpec)
{
    public string Vs { get; } = vs;
    public string Fs { get; } = fs;
    public AssetFileSpec VertexFileSpec { get; } = vertSpec;
    public AssetFileSpec FragmentFileSpec { get; } = fragSpec;
}