#region

using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

internal static class DebugLog
{
    public static GfxDebugLog MakeAddGfxStore(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Gfx, GfxLogSource.Store, GfxLogAction.Add);

    public static GfxDebugLog MakeRemoveGfxStore(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Gfx, GfxLogSource.Store, GfxLogAction.Remove);

    public static GfxDebugLog MakeReplaceGfxStore(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Gfx, GfxLogSource.Store, GfxLogAction.Replace);

    public static GfxDebugLog MakeAddBackendStore(uint handle, GfxHandle h)
        => new((int)handle, h.Slot, h.Gen, h.Kind, GfxLogLayer.Backend, GfxLogSource.Store, GfxLogAction.Add);

    public static GfxDebugLog MakeRemoveBackendStore(uint handle, GfxHandle h)
        => new((int)handle, h.Slot, h.Gen, h.Kind, GfxLogLayer.Backend, GfxLogSource.Store, GfxLogAction.Remove);

    public static GfxDebugLog MakeReplaceBackendStore(uint handle, GfxHandle h)
        => new((int)handle, h.Slot, h.Gen, h.Kind, GfxLogLayer.Backend, GfxLogSource.Store, GfxLogAction.Replace);


    public static GfxDebugLog MakeShaderCompile(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Gfx, GfxLogSource.Shader, GfxLogAction.Compile);

    public static GfxDebugLog MakeProgramLink(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Gfx, GfxLogSource.Program, GfxLogAction.Link);

    public static GfxDebugLog MakeMeshUpload(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Gfx, GfxLogSource.Mesh, GfxLogAction.Upload);

    public static GfxDebugLog MakeBufferMap(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Gfx, GfxLogSource.Buffer, GfxLogAction.Map);

    public static GfxDebugLog MakeResourceDispose(in DeleteResourceCommand cmd)
    {
        var handle = (int)cmd.BackendHandle.Value;
        var h = cmd.Handle;
        return new GfxDebugLog(handle, h.Slot, h.Gen, h.Kind, GfxLogLayer.Backend, GfxLogSource.Resource,
            GfxLogAction.Dispose);
    }

    public static GfxDebugLog MakeEnqueueDispose(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Backend, GfxLogSource.Resource, GfxLogAction.EnqueueDispose);

    public static GfxDebugLog MakeFboInvalid(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Gfx, GfxLogSource.Framebuffer, GfxLogAction.Invalidate);

    public static GfxDebugLog MakeStateChange(int id, GfxHandle h)
        => new(id, h.Slot, h.Gen, h.Kind, GfxLogLayer.Gfx, GfxLogSource.State, GfxLogAction.SetState);
}