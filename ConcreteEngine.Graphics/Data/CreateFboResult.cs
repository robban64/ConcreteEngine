namespace ConcreteEngine.Graphics.Data;

public readonly struct CreateFboResult(ushort fboId, ushort colTexId)
{
    public readonly ushort FboId = fboId;
    public readonly ushort ColTexId = colTexId;
}
