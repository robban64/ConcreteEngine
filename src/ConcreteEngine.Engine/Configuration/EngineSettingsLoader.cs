using System.Text.Json;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Diagnostics;

namespace ConcreteEngine.Engine.Configuration;

internal static class EngineSettingsLoader
{
    public static void LoadGraphicSettings()
    {
        var path = AssetPaths.GraphicSettingsFilePath;
        if (!File.Exists(path))
        {
            Logger.LogString(LogScope.Engine, "Loading Default Engine Settings...");
            EngineSettings.LoadSettings(new EngineSettingsRecord());
        }

        Logger.LogString(LogScope.Engine, "Loading Engine Settings...");

        var options = JsonUtility.DefaultJsonOptions;
        var record = JsonSerializer.Deserialize<EngineSettingsRecord>(File.ReadAllText(path), options) ??
                     throw new InvalidDataException("Invalid Engine Settings.");

        EngineSettings.LoadSettings(record);
    }
}