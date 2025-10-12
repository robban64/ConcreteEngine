using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.Assets.Shaders;

internal record struct ShaderCreationInfo(ShaderId ShaderId, int Samplers);

internal sealed record ShaderPayload(
    string Vs,
    string Fs,
    in AssetFileSpec VertexFileSpec,
    in AssetFileSpec FragmentFileSpec);
