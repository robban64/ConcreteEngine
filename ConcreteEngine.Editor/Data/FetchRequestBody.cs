#region

using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.Data;

public sealed record EntityRequestBody(int EntityId);

public sealed record CameraRequestBody(long Generation);

public sealed record AssetRequestBody(int AssetId);

public sealed record AssetCategoryRequestBody(EditorAssetCategory Category);