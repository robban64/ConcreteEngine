using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;

namespace ConcreteEngine.Editor.Data;

public struct TransformEditorModel(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public Vector3 Translation = translation;
    public Vector3 Scale = scale;
    public Quaternion Rotation = rotation;
}

public struct EntityEditorModel(int modelId, int materialTagKey, int drawCount)
{
    public int ModelId = modelId;
    public int MaterialTagKey = materialTagKey;
    public int DrawCount = drawCount;
}

public struct ProjectionEditorModel(float aspectRatio, float fov, float near, float far)
{
    public readonly float AspectRatio = aspectRatio;
    public float Fov = fov;
    public float Near = near;
    public float Far = far;
}





