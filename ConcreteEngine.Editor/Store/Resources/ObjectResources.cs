namespace ConcreteEngine.Editor.Store.Resources;

public sealed class EditorParticleResource : EditorResource
{
    public EditorId MeshId { get; set; }
}

public sealed class EditorAnimationResource : EditorResource
{
    public EditorId ModelId { get; set; }
    public EditorAnimationClip[] Clips = [];

}

public sealed class EditorAnimationClip
{
    public string DisplayName {get; set;} = string.Empty;
    public float Duration { get; set; }
    public float TicksPerSecond { get; set; }
    public int TrackCount { get; set; }
}