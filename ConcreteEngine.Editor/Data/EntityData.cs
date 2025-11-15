namespace ConcreteEngine.Editor.Data;

public readonly struct EditorEntityModel(int modelId, int materialTagKey, int drawCount)
{
    public readonly int ModelId = modelId;
    public readonly int MaterialTagKey = materialTagKey;
    public readonly int DrawCount = drawCount;
}