namespace Generator.InspectorGen;

internal readonly record struct AccessPath(string Owner, string Value, bool UsesStructCopy);

internal sealed class EmitSegment
{
    public string? Name { get; }
    public string DisplayName { get; }
    public string FieldName { get; }
    public InspectorMember[] Members { get; }

    public EmitSegment(InspectorGroup group, string? name, InspectorMember[] members)
    {
        Name = name;
        Members = members;

        if (group.HasRootTarget) DisplayName = (IsDefault ? "Root" : Name!);
        else if (IsDefault) DisplayName = group.Name;
        else DisplayName = $"{group.Name}_{Name!}";

        FieldName = $"Section{DisplayName}";
    }

    public bool IsDefault => string.IsNullOrEmpty(Name);
}

internal readonly record struct EmitGroup(InspectorGroup Group, EmitSegment[] Segments);

internal sealed class EmitterModel
{
    public readonly InspectModel Model;
    public readonly EmitGroup[] GroupSegments;
    public readonly List<EmitSegment> Segments = [];
    public readonly string NestedClassName;
    public readonly string NestedClassField;

    public string InspectorName => Model.InspectorName;
    public string InspectorNs => Model.InspectorNs;
    public string TargetName => Model.TargetName;
    public string TargetNs => Model.TargetNs;

    public EmitterModel(InspectModel model)
    {
        Model = model;
        NestedClassName = $"{model.InspectorName}{InspectorGeneratorEmitter.FieldClassSuffix}";
        NestedClassField = $"Instance.{InspectorGeneratorEmitter.FieldClassFieldName}";
        var groupSegments = new EmitGroup[model.Groups.Length];
        for (var i = 0; i < groupSegments.Length; i++)
        {
            var group = model.Groups[i];
            var segment = BuildSegments(group);
            groupSegments[i] = new EmitGroup(group, segment);
            Segments.AddRange(segment);
        }

        GroupSegments = groupSegments;
    }


    private static EmitSegment[] BuildSegments(InspectorGroup group)
    {
        var dict = new Dictionary<string, List<InspectorMember>>(StringComparer.Ordinal);

        foreach (var member in group.Members.AsSpan())
        {
            var key = member.Segment ?? "";

            if (!dict.TryGetValue(key, out var list)) dict[key] = list = new List<InspectorMember>();
            list.Add(member);
        }


        var segments = new List<EmitSegment>(dict.Count);
        if (dict.Remove("", out var defaultMembers))
            segments.Add(new EmitSegment(group, null, defaultMembers.ToArray()));

        foreach (var pair in dict)
            segments.Add(new EmitSegment(group, pair.Key, pair.Value.ToArray()));

        return segments.ToArray();
    }
    
}