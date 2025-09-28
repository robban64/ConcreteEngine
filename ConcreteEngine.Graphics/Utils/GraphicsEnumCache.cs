#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Utils;

public static class GraphicsEnumCache
{
    public static readonly ShaderUniform[] ShaderUniformVals = Enum.GetValues<ShaderUniform>();
}