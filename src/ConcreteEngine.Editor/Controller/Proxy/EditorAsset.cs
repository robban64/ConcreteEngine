using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Editor.Controller.Proxy;

public abstract class EditorAsset(AssetFileSpec[] fileSpecs)
{
    public abstract AssetObject Asset { get; }
    public readonly AssetFileSpec[] FileSpecs = fileSpecs;
}

public struct MaterialTextureInfo
{
     
}
public class EditorMaterial(Material asset, AssetFileSpec[] fileSpecs)  : EditorAsset(fileSpecs) 
{
    public override Material Asset { get; } = asset;
}

public class EditorModel(Model asset, AssetFileSpec[] fileSpecs)  : EditorAsset(fileSpecs) 
{
    public override Model Asset { get; } = asset;
}

public class EditorTexture(Texture asset, AssetFileSpec[] fileSpecs)  : EditorAsset(fileSpecs) 
{
    public override Texture Asset { get; } = asset;
}

public class EditorShader(Shader asset, AssetFileSpec[] fileSpecs)  : EditorAsset(fileSpecs) 
{
    public override Shader Asset { get; } = asset;
}


/*

 [MethodImpl(MethodImplOptions.AggressiveInlining)]
 internal void Draw(in FrameContext ctx)
 {
     var sw = ctx.Writer;
     var items = Inspector.Items;
     foreach (var item in items)
     {
         ImGui.Spacing();
         ImGui.TextUnformatted(ref sw.Write(item.FieldName));
         if (item.Info.Length > 0)
         {
             ImGui.SameLine();
             ImGui.TextUnformatted(ref sw.Start('[').Append(item.Info).Append(']').End());
         }

         ImGui.Separator();
         item.Draw(in ctx);
     }
     Inspector.EndFrame();
 }
}*/
