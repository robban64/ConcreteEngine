using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Extensions;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Graphics.Resources;

public enum UniformGpuSlot
{
    Frame = 0,
    Camera = 1,
    DirLight = 2,
    Material = 3,
    DrawObject = 4
}

public enum UboDefaultCapacity
{
    Lower,
    Medium,
    Upper
}

public interface IUniformGpuData;

public readonly record struct UniformBufferDescriptor<T>(
    UniformGpuSlot Slot,
    UboDefaultCapacity DefaultCapacity
) where T : unmanaged, IUniformGpuData;