#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlShader :  IShader
{
    public uint Handle { get; }
    public bool IsDisposed { get; set; } = false;

    internal GlShader(uint handle)
    {
        Handle = handle;
    }
}