using System.Numerics;

namespace Core.DebugTools.Data;

public struct EntityEditorTransform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public Vector3 Translation = translation;
    public Vector3 Scale = scale;
    public Vector3 EulerAngles = Vector3.Zero;
    public Quaternion Rotation = rotation;
}

public struct EntityEditorModel(int modelId, int materialTagKey, int drawCount)
{
    public int ModelId = modelId;
    public int MaterialTagKey = materialTagKey;
    public int DrawCount = drawCount;
}

