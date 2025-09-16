namespace ConcreteEngine.Graphics.Resources;

internal interface IBackendOps
{
    ResourceKind Kind { get; }
    void Delete(in GfxHandle h);
}
