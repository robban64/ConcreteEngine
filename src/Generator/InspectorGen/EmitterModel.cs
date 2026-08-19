namespace Generator.InspectorGen;

internal readonly record struct AccessPath(string Owner, string Value, bool UsesStructCopy);

internal readonly record struct EmitSegment(string? Name, InspectorMember[] Members)
{
    public bool IsDefault => string.IsNullOrEmpty(Name);

    public void AppendDrawName(SourceBuilder sb, InspectorGroup group)
    {
        if (group.HasRootTarget) sb.Append(IsDefault ? "Root" : Name!); // DrawRoot, DrawSegment
        else if (IsDefault) sb.Append(group.Name); // DrawGroup
        else sb.Builder.Append($"{group.Name}_{Name!}"); //DrawGroup_Segment
    }
}

internal readonly record struct EmitGroup(InspectorGroup Group, EmitSegment[] Segments);

internal sealed class EmitterModel(InspectModel model, EmitGroup[] groups)
{
    public InspectModel Model = model;
    public EmitGroup[] Groups = groups;
    public string NestedClassName = $"{model.InspectorName}Fields";
}
