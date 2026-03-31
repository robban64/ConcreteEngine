using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Extensions;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;

namespace ConcreteEngine.Editor.UI.Assets;


internal sealed class AssetListState(AssetKind pendingKind)
{
    public const string GoBackString = "..";
    public AssetFileId SelectedFileId { get; set; }
    public AssetKind SelectedKind { get; private set; }
    public AssetKind PendingKind { get; private set; } = pendingKind;
    public bool IsRootPath { get; private set; }
    public string? PendingDirectory { get; private set; }

    public Vector4 CurrentColor = Color4.White;
    
    public NativeViewPtr<byte> BreadcrumbStrPtr;

    public void EnqueueNewAssetKind(AssetKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero((int)kind, nameof(kind));
        if (PendingKind != AssetKind.Unknown)
            throw new InvalidOperationException($"Already pending. Pending kind: {PendingKind.ToText()}");
        if (PendingDirectory != null)
            throw new InvalidOperationException($"Already pending. Pending directory: {PendingDirectory}");

        PendingKind = kind;
    }

    public void EnqueueDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        if (PendingKind != AssetKind.Unknown)
            throw new InvalidOperationException($"Already pending. Pending kind: {PendingKind.ToText()}");
        if (PendingDirectory != null)
            throw new InvalidOperationException($"Already pending. Pending directory: {PendingDirectory}");

        PendingDirectory = directory;
    }
    
    public void UpdateTitleText(AssetBrowser assetBrowser)
    {
        var dirSpan = assetBrowser.CurrentDirectory.AsSpan();
        var sw = BreadcrumbStrPtr.Writer();
        sw.Append('[').Append(assetBrowser.FilteredCount).Append(']').PadRight(2).Append('/');
        foreach (var range in dirSpan.Split('/'))
            sw.Append(dirSpan[range]).Append('/');

        // remove last '/'
        sw.SetCursor(sw.Cursor - 1);
        sw.Append((char)0);
    }


    public bool SyncStateToBrowser(AssetBrowser assetBrowser)
    {
        if (PendingKind == AssetKind.Unknown && PendingDirectory == null) return false;

        if (PendingKind != AssetKind.Unknown && PendingKind != SelectedKind)
        {
            SelectedKind = PendingKind;
            CurrentColor = StyleMap.GetAssetColor(SelectedKind);
            PendingKind = AssetKind.Unknown;
            PendingDirectory = SelectedKind.ToRootFolder();
        }

        SetAssetDirectory(assetBrowser);
        return true;
    }

    private void SetAssetDirectory(AssetBrowser assetBrowser)
    {
        if (SelectedKind == AssetKind.Unknown || PendingDirectory == null) return;

        IsRootPath = SelectedKind.ToRootFolder() == PendingDirectory;
        var hasPath = !IsRootPath && PendingDirectory.IndexOf('/') > 0;

        if (IsRootPath || hasPath)
            assetBrowser.SetDirectory(PendingDirectory, SelectedKind);
        else if (PendingDirectory == GoBackString)
            assetBrowser.SetToParentDirectory();
        else
            assetBrowser.SetLocalDirectory(PendingDirectory);

        UpdateTitleText(assetBrowser);
        PendingDirectory = null;
    }

}