using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class SceneObjectBlueprint
{
    public string DisplayName = string.Empty;
    public Guid GId = Guid.NewGuid();
    public bool IsDirty;
}
