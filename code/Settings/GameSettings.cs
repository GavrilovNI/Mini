using Sandbox;
using System.Collections.Generic;

namespace Mini.Settings;

public class GameSettings
{
    public const string DefaultPath = "settings.json";

    private static GameSettings? _current;
    public static GameSettings Current => _current ??= Load(DefaultPath);


    [DefaultValue(8f)]
    public float Sensitivity { get; set; } = 8f;


    public GameSettings()
    {
    }

    public void Save(string path)
    {
        var json = Json.Serialize(this);
        FileSystem.Data.WriteAllText(path, json);
    }

    public static GameSettings Load(string path)
    {
        if(!FileSystem.Data.FileExists(path))
            return new();

        var json = FileSystem.Data.ReadAllText(path);
        return Json.Deserialize<GameSettings>(json);
    }
}
