namespace ConcreteEngine.Core.Engine.Scene;

public abstract class BlueprintInstance
{ }

public abstract class BlueprintInstance<TBlueprint> : BlueprintInstance where TBlueprint : ComponentBlueprint
{ }

public sealed class ModelRuntimeBlueprint : BlueprintInstance<ModelBlueprint>
{ }