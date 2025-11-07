#region

using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Assets.Shaders;

internal record struct ShaderCreationInfo(ShaderId ShaderId, int Samplers);

internal sealed record ShaderPayload(
    string Vs,
    string Fs,
    in AssetFileSpec VertexFileSpec,
    in AssetFileSpec FragmentFileSpec);