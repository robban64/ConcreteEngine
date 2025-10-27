#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.RenderingSystem.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Scene.Entities;

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntity(
    in ModelComponent model,
    in Transform transform,
    DrawCommandId commandId,
    DrawCommandQueue queue,
    PassMask passMask,
    ushort depthKey = 0)
{
    public Transform Transform = transform;
    public ModelId Model = model.Model;
    public MaterialId Material = model.Material;
    public int DrawCount = model.DrawCount;
    public PassMask PassPassMask = passMask;
    public ushort DepthKey = depthKey;
    public DrawCommandId CommandId = commandId;
    public DrawCommandQueue Queue = queue;

    // ensure 64-byte
    private int _pad;

}