#region

global using StateContext = ConcreteEngine.Editor.EditorStateContext;
global using ModelManager = ConcreteEngine.Editor.EditorModelManager;

global using unsafe WorldParamStateFp = delegate*<ref ConcreteEngine.Editor.DataState.WorldParamState, void>;
global using unsafe CameraStateFp = delegate*<ref ConcreteEngine.Editor.Data.CameraEditorPayload, void>;
global using unsafe EntityStateFp = delegate*<ref ConcreteEngine.Editor.Data.EntityDataPayload, void>;

#endregion