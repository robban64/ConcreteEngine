#region

using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Scene.Entities;

[StructLayout(LayoutKind.Sequential)]
public struct DrawEntity(
    in MeshComponent mesh,
    in Transform transform,
    DrawCommandId commandId,
    DrawCommandQueue queue,
    PassMask passMask,
    ushort depthKey = 0)
{
    public Transform Transform = transform;
    public MeshId MeshId = mesh.MeshId;
    public MaterialId MaterialId = mesh.MaterialId;
    public int DrawCount = mesh.DrawCount;
    public PassMask PassPassMask = passMask;
    public ushort DepthKey = depthKey;
    public DrawCommandId CommandId = commandId;
    public DrawCommandQueue Queue = queue;
}