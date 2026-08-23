using Generator.Misc;

namespace Generator.InspectorGen;

internal sealed record InspectModel(
    string InspectorName,
    string InspectorNs,
    string TargetName,
    string TargetNs,
    EquatableArray<InspectorGroup> Groups)
{
    public string? DisplayName { get; init; }
}

internal sealed record InspectorGroup(
    bool HasRootTarget,
    string Name,
    string AccessPath,
    MemberInfo Info,
    EquatableArray<InspectorMember> Members);

internal sealed record InspectorMember(
    string Name,
    string Label,
    string TargetNs,
    string TypeName,
    MemberInfo Info)
{
    public string? Segment { get; init; }
    public InputField? Input { get; init; }
}