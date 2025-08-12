#region

using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Assets;

public interface IGraphicAssetFile : IAssetFile
{
    public ushort ResourceId { get; init; }
}