using System.Numerics;
using ConcreteEngine.Core.Renderer.Data;

namespace ConcreteEngine.Core.Renderer;

public sealed class CameraTransforms
{
    public bool UseLightSpace;
    public Vector3 Translation;
    public CameraMatrices FrameMatrices = CameraMatrices.CreateIdentity();
    public CameraMatrices LightMatrices = CameraMatrices.CreateIdentity();
}